# -*- coding: utf-8 -*-
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', line_buffering=True)
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', line_buffering=True)

import os
import cv2
import mediapipe as mp
import socket
import struct
import threading
import time
import json
import math

HOST       = "127.0.0.1"
PORT       = 9999   # Unity TCP server — nhận dữ liệu điều khiển JSON
FRAME_PORT = 9998   # Python TCP server — stream JPEG preview

# ────────────────────────────────────────────────────────────────────
# CẤU HÌNH TAY
# MediaPipe gán nhãn từ góc nhìn người dùng (không phụ thuộc camera flip).
# "Right" = tay phải của người dùng (xuất hiện bên TRÁI frame chưa flip).
# ────────────────────────────────────────────────────────────────────
COMMAND_HAND = "Right"   # tay TRÁI người dùng* điều khiển xe  (* xem ghi chú dưới)
POINTER_HAND = "Left"    # tay PHẢI người dùng* điều ngắm/crosshair

# ** Nếu bắt nhầm tay, đổi 2 giá trị trên thành "Left"/"Right" **

# ────────────────────────────────────────────────────────────────────
# CẤU HÌNH ĐIỀU KHIỂN GÓC NGHIÊNG CỔ TAY (Wrist-angle mode)
#
#  Dùng vector cổ tay (lm[0]) → gốc ngón giữa (lm[9]) làm "la bàn":
#
#          ny=+1  (tay chỉ lên → TIẾN)
#            ↑
#   nx=-1 ←──┼──→ nx=+1
#            ↓
#          ny=-1  (tay chỉ xuống → LÙI)
#
#  Dead zone ở giữa ngăn lệnh nhảy lung tung khi tay gần thẳng đứng.
#
#  Không cần đặt tay vào vùng cố định — chỉ nghiêng cổ tay là đủ.
# ────────────────────────────────────────────────────────────────────
TILT_FORWARD_THRESHOLD = 0.30   # |ny| phải vượt ngưỡng này mới tiến/lùi
TILT_TURN_THRESHOLD    = 0.30   # |nx| phải vượt ngưỡng này mới rẽ

# Tay phải: 1 ngón = chỉ aim, 2 ngón = aim + bắn
POINTER_SHOOT_FINGERS = 2

# Debounce shoot (tay phải)
SHOOT_CONFIRM_FRAMES = 2

# Chống rung: cần N frame liên tiếp giống nhau mới xác nhận
CMD_CONFIRM_FRAMES = 2
# Timeout: mất tay N frame thì về STOP
CMD_TIMEOUT_FRAMES = 6
# EMA smooth cho pointer
POINTER_ALPHA = 0.25
# Gửi preview mỗi N frame
FRAME_SKIP = 2

# ────────────────────────────────────────────────────────────────────
# KHỞI TẠO MEDIAPIPE & CAMERA
# ────────────────────────────────────────────────────────────────────
print("Initializing MediaPipe...")
mp_hands = mp.solutions.hands
hands    = mp_hands.Hands(
    max_num_hands=2,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.6,
)
mp_draw = mp.solutions.drawing_utils
mp_style = mp.solutions.drawing_styles


def _try_open_capture(index: int):
    """Mở webcam theo index; Windows ưu tiên DirectShow (thường ổn định hơn MSMF)."""
    if sys.platform == "win32" and hasattr(cv2, "CAP_DSHOW"):
        cap = cv2.VideoCapture(index, cv2.CAP_DSHOW)
        if cap.isOpened():
            ok, frame = cap.read()
            if ok and frame is not None and getattr(frame, "size", 0) > 0:
                return cap
        cap.release()
    cap = cv2.VideoCapture(index)
    if not cap.isOpened():
        return None
    ok, frame = cap.read()
    if ok and frame is not None and getattr(frame, "size", 0) > 0:
        return cap
    cap.release()
    return None


def open_webcam():
    """Thử HAND_CAM_INDEX (env), rồi quét index 0..4. In hướng dẫn nếu không có cam."""
    raw = (os.environ.get("HAND_CAM_INDEX") or "").strip()
    if raw.isdigit() or (raw.startswith("-") and raw[1:].isdigit()):
        i = int(raw)
        cap = _try_open_capture(i)
        if cap is not None:
            print(f"Camera ready (HAND_CAM_INDEX={i}).")
            return cap
        print(f"[Camera] HAND_CAM_INDEX={i} không mở được, đang thử index khác...", file=sys.stderr)

    for i in range(5):
        cap = _try_open_capture(i)
        if cap is not None:
            print(
                f"Camera ready (device index {i}). "
                f"Để ghim: đặt biến môi trường HAND_CAM_INDEX={i} trước khi chạy script."
            )
            return cap

    print(
        "ERROR: Không mở được webcam. Kiểm tra: Windows Cài đặt → Quyền riêng tư → Camera "
        "(bật cho ứng dụng desktop); tắt app khác đang dùng cam; thử HAND_CAM_INDEX=1 hoặc 2.",
        file=sys.stderr,
    )
    sys.exit(1)


cap = open_webcam()
cap.set(cv2.CAP_PROP_FRAME_WIDTH,  640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

# ────────────────────────────────────────────────────────────────────
# FRAME STREAM SERVER (port 9998)
# ────────────────────────────────────────────────────────────────────
_frame_clients_lock = threading.Lock()
_frame_clients      = []

def _frame_server_loop():
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind((HOST, FRAME_PORT))
    srv.listen(5)
    print(f"[FrameServer] Listening on port {FRAME_PORT}...")
    while True:
        try:
            conn, _ = srv.accept()
            conn.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            with _frame_clients_lock:
                _frame_clients.append(conn)
            print("[FrameServer] Unity connected for frame preview.")
        except Exception as e:
            print("[FrameServer] Error:", e)
            break

threading.Thread(target=_frame_server_loop, daemon=True).start()

def _send_frame_to_clients(jpeg_bytes):
    header  = struct.pack('>I', len(jpeg_bytes))
    payload = header + jpeg_bytes
    dead = []
    with _frame_clients_lock:
        for c in _frame_clients:
            try:    c.sendall(payload)
            except: dead.append(c)
        for c in dead:
            try: c.close()
            except: pass
            _frame_clients.remove(c)

# ────────────────────────────────────────────────────────────────────
# ĐẾM NGÓN TAY (0-5, kể cả ngón cái)
# Dùng cho tay TRÁI (command hand) — cần phân biệt nắm tay (0) vs mở (5)
# ────────────────────────────────────────────────────────────────────
def count_fingers(lms, hand_label):
    lm    = lms.landmark
    count = 0
    # Ngón cái: so sánh x (kém ổn định, nhưng ok cho tay trái vì chỉ cần 0 vs 5)
    if hand_label == "Right":
        if lm[4].x > lm[2].x: count += 1
    else:
        if lm[4].x < lm[2].x: count += 1
    # 4 ngón còn lại: đầu ngón cao hơn khớp pip
    for tip_id in [8, 12, 16, 20]:
        if lm[tip_id].y < lm[tip_id - 2].y:
            count += 1
    return count


# ────────────────────────────────────────────────────────────────────
# ĐẾM NGÓN TAY KHÔNG TÍNH NGÓN CÁI (0-4)
# Dùng cho tay PHẢI (pointer hand) — chỉ cần phân biệt 1 vs 2 ngón.
# Bỏ ngón cái vì so sánh trục x rất nhạy với góc xoay tay → gây lỗi.
# So sánh y (đầu ngón cao hơn khớp PIP) ổn định hơn nhiều.
# ────────────────────────────────────────────────────────────────────
def count_fingers_no_thumb(lms):
    lm    = lms.landmark
    count = 0
    for tip_id in [8, 12, 16, 20]:
        if lm[tip_id].y < lm[tip_id - 2].y:
            count += 1
    return count

# ────────────────────────────────────────────────────────────────────
# ĐIỀU KHIỂN THEO GÓC NGHIÊNG CỔ TAY
# Trả về (move, turn): move ∈ {1,0,-1}, turn ∈ {1,0,-1}
# ────────────────────────────────────────────────────────────────────
def get_zone_by_angle(lms):
    """
    Dùng vector lm[0](cổ tay) → lm[9](gốc ngón giữa) làm hướng điều khiển.
    Không phụ thuộc vị trí tay trong frame — chỉ cần góc nghiêng.
    """
    lm = lms.landmark
    dx =   lm[9].x - lm[0].x        # dương = ngón giữa lệch phải so với cổ tay
    dy = -(lm[9].y - lm[0].y)       # đảo y: image y tăng xuống → ta muốn dương = lên

    length = math.sqrt(dx * dx + dy * dy)
    if length < 0.05:                # tay quá sát camera / tracking không ổn
        return 0, 0

    nx = dx / length    # thành phần ngang chuẩn hoá [-1, +1]
    ny = dy / length    # thành phần dọc chuẩn hoá  [-1, +1]

    # Tiến / Lùi theo thành phần dọc (đảo dấu so với image-space)
    if   ny < -TILT_FORWARD_THRESHOLD: move =  -1   # tay chỉ xuống → tiến
    elif ny >  TILT_FORWARD_THRESHOLD: move = 1   # tay chỉ lên   → lùi
    else:                               move =  0

    # Rẽ theo thành phần ngang (đảo dấu so với image-space)
    if   nx >  TILT_TURN_THRESHOLD: turn =  1   # nghiêng phải → rẽ trái
    elif nx < -TILT_TURN_THRESHOLD: turn = -1   # nghiêng trái → rẽ phải
    else:                            turn =  0

    return move, turn

# Zone name để hiển thị debug
_ZONE_NAMES = {
    ( 1,  1): "FWD-L",  ( 1, 0): "FWD",  ( 1, -1): "FWD-R",
    (-1,  1): "BWD-L",  (-1, 0): "BWD", (-1, -1): "BWD-R",
    ( 0,  0): "STOP",
}

# ────────────────────────────────────────────────────────────────────
# KẾT NỐI UNITY (port 9999)
# ────────────────────────────────────────────────────────────────────
print("Connecting to Unity on port 9999...")
client = None
while True:
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.connect((HOST, PORT))
        client = s
        print("Da ket noi Unity!")
        break
    except Exception:
        try: s.close()
        except: pass
        time.sleep(0.5)

# ────────────────────────────────────────────────────────────────────
# VÒNG LẶP CHÍNH
# ────────────────────────────────────────────────────────────────────
# Pointer (tay phải người dùng) — EMA smooth
smooth_px     = 0.5
smooth_py     = 0.5
pointer_active = False

# Command state — movement debounce (tay trái)
confirmed_move  = 0
confirmed_turn  = 0
cand_move       = 0
cand_turn       = 0
cand_count      = 0
timeout_frames  = 0

# Shoot debounce (tay phải)
confirmed_shoot  = False
shoot_cand_count = 0

last_sent_json = ""
frame_counter  = 0

# Lưu thông tin để vẽ preview
dbg_fingers     = -1
dbg_ptr_fingers = -1   # số ngón tay phải
dbg_zone_name   = "---"
dbg_wrist_px    = -1   # toạ độ wrist trên preview (sau mirror+resize)
dbg_wrist_py    = -1
dbg_mid_px      = -1   # toạ độ gốc ngón giữa trên preview
dbg_mid_py      = -1

while True:
    success, img = cap.read()
    if not success:
        continue

    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = hands.process(img_rgb)

    h_img, w_img = img.shape[:2]

    # ── Phân tách 2 tay ──────────────────────────────────────────
    cmd_detected    = False
    new_move        = 0
    new_turn        = 0
    new_shoot       = False
    ptr_raw         = None
    dbg_ptr_fingers = -1

    if results.multi_hand_landmarks and results.multi_handedness:
        for lms, handedness in zip(results.multi_hand_landmarks,
                                   results.multi_handedness):
            label = handedness.classification[0].label

            mp_draw.draw_landmarks(img, lms, mp_hands.HAND_CONNECTIONS)

            if label == COMMAND_HAND:
                # Tay trái: điều khiển di chuyển bằng góc nghiêng cổ tay
                wrist        = lms.landmark[0]
                mid_mcp      = lms.landmark[9]
                dbg_fingers  = -1   # không dùng gesture nữa

                cmd_detected      = True
                new_move, new_turn = get_zone_by_angle(lms)
                dbg_zone_name     = _ZONE_NAMES.get((new_move, new_turn), "STOP")

                # Vẽ vector hướng tay trên preview (cổ tay → gốc ngón giữa)
                dbg_wrist_px  = int((1 - wrist.x)   * 320)
                dbg_wrist_py  = int(wrist.y          * 240)
                dbg_mid_px    = int((1 - mid_mcp.x)  * 320)
                dbg_mid_py    = int(mid_mcp.y         * 240)

            elif label == POINTER_HAND:
                # Tay phải: đếm ngón KHÔNG tính ngón cái (ổn định hơn)
                # 1 ngón = chỉ aim, 2 ngón = aim + bắn
                fingers_ptr     = count_fingers_no_thumb(lms)
                dbg_ptr_fingers = fingers_ptr
                tip             = lms.landmark[8]
                ptr_raw         = (tip.x, tip.y)
                new_shoot       = (fingers_ptr == POINTER_SHOOT_FINGERS)

    # ── Debounce movement (tay trái) ─────────────────────────────
    if cmd_detected:
        timeout_frames = 0
        if (new_move, new_turn) == (cand_move, cand_turn):
            cand_count += 1
        else:
            cand_move, cand_turn = new_move, new_turn
            cand_count = 1

        if cand_count >= CMD_CONFIRM_FRAMES:
            confirmed_move = cand_move
            confirmed_turn = cand_turn
    else:
        timeout_frames += 1
        if timeout_frames >= CMD_TIMEOUT_FRAMES:
            confirmed_move  = 0
            confirmed_turn  = 0
            cand_move = cand_turn = 0
            cand_count = 0
            dbg_zone_name   = "STOP"
            dbg_fingers     = -1

    # ── Debounce shoot (tay phải) ─────────────────────────────────
    if new_shoot:
        shoot_cand_count += 1
        if shoot_cand_count >= SHOOT_CONFIRM_FRAMES:
            confirmed_shoot = True
    else:
        shoot_cand_count = 0
        confirmed_shoot  = False

    # ── Pointer smooth ───────────────────────────────────────────
    if ptr_raw is not None:
        pointer_active = True
        smooth_px = POINTER_ALPHA * ptr_raw[0] + (1 - POINTER_ALPHA) * smooth_px
        smooth_py = POINTER_ALPHA * ptr_raw[1] + (1 - POINTER_ALPHA) * smooth_py
    else:
        pointer_active = False

    # ── Gửi JSON ─────────────────────────────────────────────────
    packet = {
        "move":  confirmed_move,
        "turn":  confirmed_turn,
        "shoot": 1 if confirmed_shoot else 0,
        "px":    round(smooth_px, 4),
        "py":    round(smooth_py, 4),
        "pa":    1 if pointer_active else 0,
    }
    line = json.dumps(packet, separators=(',', ':'))
    if line != last_sent_json:
        try:
            client.send((line + "\n").encode())
            last_sent_json = line
            print("Send:", line)
        except Exception:
            print("Connection lost. Exiting.")
            break

    # ── Gửi FRAME PREVIEW ────────────────────────────────────────
    frame_counter += 1
    if frame_counter >= FRAME_SKIP and _frame_clients:
        frame_counter = 0
        preview = cv2.flip(img, 1)
        preview = cv2.resize(preview, (320, 240))

        # Vẽ vector hướng tay (cổ tay → gốc ngón giữa) thay vì lưới zone
        if 0 <= dbg_wrist_px < 320:
            active_color = (0, 255, 80) if (confirmed_move != 0 or confirmed_turn != 0) \
                           else (80, 80, 255)
            # Đường vector
            cv2.arrowedLine(preview,
                            (dbg_wrist_px, dbg_wrist_py),
                            (dbg_mid_px,   dbg_mid_py),
                            active_color, 2, tipLength=0.35)
            # Dot cổ tay
            cv2.circle(preview, (dbg_wrist_px, dbg_wrist_py), 7, active_color, -1)
            cv2.circle(preview, (dbg_wrist_px, dbg_wrist_py), 9, (255,255,255), 2)

        # Dot vị trí pointer hand (ngón trỏ)
        if pointer_active:
            px_p = int((1 - smooth_px) * 320)
            py_p = int(smooth_py * 240)
            cv2.circle(preview, (px_p, py_p), 8, (0, 100, 255), -1)

        # HUD thông tin
        fp_text = f"R:{dbg_ptr_fingers}" if dbg_ptr_fingers >= 0 else "R:--"
        s_text  = " SHOOT" if confirmed_shoot else ""
        cv2.putText(preview, f"{dbg_zone_name}  {fp_text}{s_text}",
                    (5, 228), cv2.FONT_HERSHEY_SIMPLEX, 0.50, (0,230,80), 2)

        ok, jpg = cv2.imencode('.jpg', preview, [cv2.IMWRITE_JPEG_QUALITY, 60])
        if ok:
            _send_frame_to_clients(jpg.tobytes())

    time.sleep(0.01)

cap.release()
if client:
    try: client.close()
    except: pass
print("Done.")
