# Tiên Lộ Kỳ Duyên 2D

Bản Unity/C# của bãi luyện công thử nghiệm V2. Dự án dùng các sprite PNG của sáu nhân vật trong nguyên mẫu và dựng giao diện bằng Unity UI.

## Chạy trong Unity

1. Cài Unity Editor 6000.0.23f1 bằng Unity Hub.
2. Trong Unity Hub, chọn **Add → Add project from disk**, rồi chọn thư mục gốc của kho mã.
3. Mở scene **Assets/Scenes/Training.unity** và nhấn **Play**. Giao diện được dựng khi scene chạy.
4. Để xuất bản chạy thử, mở **File → Build Profiles**, chọn nền tảng đã cài mô-đun và build scene **Training**.

Có thể dùng các tỷ lệ màn hình ngang và dọc trong cửa sổ **Game** để xem giao diện trên các cỡ màn hình. Dự án dùng Input Manager tích hợp cho bàn phím và Unity UI Event System cho chuột, cảm ứng. Nếu dự án được mở trong Editor đã đổi thiết lập đầu vào, chọn **Input Manager (Old)** hoặc **Both** trong **Project Settings → Player → Active Input Handling**.

## Điều khiển

- **1–6** hoặc chạm thẻ để chọn nhân vật.
- **A/D**, **←/→** hoặc giữ nút **Trái/Phải** để di chuyển.
- **K** hoặc nút **Chiêu thức 1** để triển khai chiêu, tốn 18 linh lực. Mỗi hình nộm trúng đòn nhận 27 sát thương và tự hồi sinh sau khi bị hạ.
- **R** hoặc nút **Làm lại** để đặt lại bãi tập.

Phần mô phỏng nằm trong `Assets/Scripts/TrainingState.cs`. Giao diện, các khung hình cùng hiệu ứng được kết nối trong `Assets/Scripts/TrainingArena.cs`.
