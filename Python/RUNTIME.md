# Python runtime cho toàn team

Mục tiêu: mọi máy dùng cùng một môi trường Python, tránh "máy chạy máy lỗi".

## Chuẩn runtime

- Python: `3.11.x` (khuyến nghị) hoặc `3.10.x`
- Dependencies: theo `requirements.txt` (đã pin version)
- Virtual env local: `Python/.venv`

## Cài trên máy mới

1. Cài Python 3.11 (hoặc 3.10) kèm `py` launcher.
2. Chạy `Python/setup_env.bat` (hoặc `setup_env.ps1`).
3. Mở Unity và bật chế độ Python hand control.

`ControlModePanel` sẽ ưu tiên chạy `Python/.venv/Scripts/python.exe` trước Python hệ thống.
