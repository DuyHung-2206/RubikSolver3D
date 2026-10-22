# 🚨 QUY TẮC BẮT BUỘC KHI LÀM VIỆC VỚI DỰ ÁN RUBIKSOLVER3D

Dự án này áp dụng 2 nguyên tắc bắt buộc tuyệt đối cho AI Assistant và các Subagent:

1. **CHỈ SỬA KHI ĐƯỢC YÊU CẦU - KHÔNG TỰ Ý SỬA ĐỔI:**
   - Chỉ được phép tạo mới hoặc chỉnh sửa file mã nguồn, cấu hình, tài nguyên khi người dùng yêu cầu rõ ràng.
   - **KHI NGƯỜI DÙNG CHAT CÓ CHỨA CHỮ "REVIEW":**
     - **TUYỆT ĐỐI KHÔNG ĐƯỢC CHỈNH SỬA, THÊM HOẶC XÓA BẤT KỲ FILE NÀO**.
     - Chỉ được phép đọc mã nguồn, phân tích, đánh giá, phát hiện lỗi và phản hồi bằng văn bản/đánh giá.
   - đưa ra đánh giá xong đối chiếu với tiến trình trong PROJECT_PROGRESS.md để xem dự án đã hoàn thiện thêm được bao nhiêu % trong phase rồi tự động cập nhật thêm tiến trình vào file md đó
2. **KHÔNG TỰ Ý ĐẨY LÊN GITHUB:**
   - Tuyệt đối **KHÔNG** tự ý thực hiện lệnh `git push` trong bất kỳ tình huống nào.
   - Chỉ được phép đẩy code lên GitHub khi người dùng chat chính xác câu lệnh:
     👉 **`"đẩy lên github"`**
3. **tiến trình trong PROJECT_PROGRESS.md chỉ được thêm không được ghi đè lên lịch sử tiến trình cũ"
   - ví dụ đang là tiến trình 21/10/2026 - 3:47 + nội dung thì khi thêm tiến trình mới phải ghi tiếp tục mà vẫn giữ lại lịch sử của tiến trình cũ