# -*- coding: utf-8 -*-
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', line_buffering=True)
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', line_buffering=True)

import os
import cv2
import mediapipe as mp
import socket
import time

HOST = "127.0.0.1"
PORT = 9999

print("Initializing MediaPipe...")
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    max_num_hands=1,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.7
)
mp_draw = mp.solutions.drawing_utils


def _try_open_capture(index: int):
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
    raw = (os.environ.get("HAND_CAM_INDEX") or "").strip()
    if raw.lstrip("-").isdigit():
        i = int(raw)
        cap = _try_open_capture(i)
        if cap is not None:
            print(f"Camera ready (HAND_CAM_INDEX={i}).")
            return cap
        print(f"[Camera] HAND_CAM_INDEX={i} failed, trying other indices...", file=sys.stderr)
    for i in range(5):
        cap = _try_open_capture(i)
        if cap is not None:
            print(f"Camera ready (index {i}). Pin with HAND_CAM_INDEX={i}.")
            return cap
    print(
        "ERROR: No webcam. Check Windows Privacy → Camera; close other apps; try HAND_CAM_INDEX=1.",
        file=sys.stderr,
    )
    sys.exit(1)


cap = open_webcam()
cap.set(3, 640)
cap.set(4, 480)

def count_fingers(lms):
    tips = [8, 12, 16, 20]
    return sum(1 for t in tips if lms.landmark[t].y < lms.landmark[t - 2].y)

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
        try:
            s.close()
        except:
            pass
        time.sleep(0.5)

last_gesture = ""

while True:
    success, img = cap.read()
    if not success:
        continue

    img = cv2.flip(img, 1)
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = hands.process(img_rgb)

    gesture = "STOP"

    if results.multi_hand_landmarks:
        for lms in results.multi_hand_landmarks:
            fingers = count_fingers(lms)

            if fingers >= 4:
                gesture = "FORWARD"
            elif fingers == 3:
                gesture = "LEFT"
            elif fingers == 2:
                gesture = "RIGHT"
            elif fingers == 1:
                gesture = "BACKWARD"
            else:
                gesture = "SHOOT"

            mp_draw.draw_landmarks(img, lms, mp_hands.HAND_CONNECTIONS)

    if gesture != last_gesture:
        try:
            client.sendall((gesture + "\n").encode("utf-8"))
            print("Send:", gesture)
            last_gesture = gesture
        except Exception as e:
            print("Connection lost. Exiting.", e)
            break

    cv2.putText(img, f"Gesture: {gesture}", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0,255,0), 2)
    cv2.putText(img, "Unity: OK", (20, 80), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0,255,0), 2)
    cv2.imshow("Hand Control", img)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

    time.sleep(0.03)

cap.release()
cv2.destroyAllWindows()

if client:
    try:
        client.close()
    except:
        pass

print("Done.")