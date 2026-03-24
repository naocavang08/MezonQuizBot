## Plan: Triển khai luồng chơi Quiz trên Website bằng SignalR

Cách tiếp cận đề xuất: Sử dụng SignalR để đồng bộ real-time giữa Host và Người chơi (Participant). Khi Host ấn Start, server sẽ đẩy dữ liệu (push event) về FE để chuyển ngay lập tức sang giao diện câu hỏi thay vì gọi API liên tục (Polling) giảm độ trễ cho game thi đấu.

**Steps**
1. **Thiết lập kết nối Real-time (Backend)**
   - Cài đặt và cấu hình thư viện/middleware SignalR trong [Program.cs](MezonQuiz/src/WebApp/Program.cs).
   - Khởi tạo thư mục/class `QuizHub.cs` để quản lý các Web Socket connections theo nhóm (Group) bằng `SessionId`.

2. **Cập nhật API & Service (Backend)** (*depends on 1*)
   - Tạo các API mới/sửa đổi API hiện có trong [QuizSessionController.cs](MezonQuiz/src/WebApp/Controllers/QuizSessionController.cs): 
     - `GET /api/QuizSession/{sessionId}/current-question`: Trả về thông tin câu hỏi, các lựa chọn (bắt buộc ẩn đáp án đúng đi).
     - `POST /api/QuizSession/{sessionId}/submit-answer`: Gửi đáp án được chọn, chấm điểm và trả kết quả. 
   - Trong [QuizSessionService.cs](MezonQuiz/src/WebApp/Application/Services/QuizSessionService.cs), tại hàm Host `StartSession()` và `NextQuestion()`, sẽ gọi lệnh SignalR Broadcast gửi các event như `SessionStarted`, `QuestionChanged` hay `SessionEnded` tới tất cả player trong SessionId group đó.

3. **Tích hợp SignalR Client (Frontend)**
   - Cài đặt package `@microsoft/signalr` vào [package.json](MezonQuizFE/package.json).
   - Khởi tạo kết nối SignalR trong [SessionRoomPage.tsx](MezonQuizFE/src/Pages/SessionRoomPage.tsx) bằng `useEffect` hoặc tạo một custom hook `useSignalR` để quản lý việc đăng ký lắng nghe và loại bỏ kết nối an toàn.

4. **Luồng điều hướng & Giao diện người chơi (Frontend)** (*depends on 3*)
   - Chỉnh sửa chức năng tham gia phòng của [FindQuizPage.tsx](MezonQuizFE/src/Pages/FindQuizPage.tsx): Sau khi Participant gọi API `joinQuizSession` thành công thay vì chỉ báo Toast, tiến hành `useNavigate("/session/" + sessionId)` để đưa User vào phòng.
   - Trong phòng chờ của Người tham gia tại [SessionRoomPage.tsx](MezonQuizFE/src/Pages/SessionRoomPage.tsx), phân thân luồng UI:
     - **Nếu user là Host:** Hiển thị Control Dashboard (Câu hỏi hiện tại, Bảng xếp hạng, Nút Start/Next/End).
     - **Nếu user là Participant:** 
       - **Trạng thái Waiting:** Hiển thị màn hình chờ "Đang chờ Host bắt đầu...".
       - **Sự kiện SessionStarted / QuestionChanged:** Bắt tín hiệu SignalR -> Hiển thị tự động nội dung câu hỏi (chọn Option A, B, C, D) và bộ đếm ngược. Sau khi User gửi đáp án, gọi API submit.
       - **Trạng thái Finished:** Hiện bảng xếp hạng (Leaderboard) cá nhân hoặc chung. Cập nhật này nên thay thế luôn hàm Polling (Interval Fetch API) hiện đang gọi API `loadSession()` mỗi 5s.

**Relevant files**
- [Program.cs](MezonQuiz/src/WebApp/Program.cs) — Đăng ký và thiết lập Endpoints cho SignalR Hub.
- [QuizSessionController.cs](MezonQuiz/src/WebApp/Controllers/QuizSessionController.cs) — Đăng ký API Get current-question & API Post submit-answer cho Player.
- [QuizSessionService.cs](MezonQuiz/src/WebApp/Application/Services/QuizSessionService.cs) — Logic bắn Push Notification của SignalR tới các client.
- [package.json](MezonQuizFE/package.json) — Install SignalR node module.
- [FindQuizPage.tsx](MezonQuizFE/src/Pages/FindQuizPage.tsx) — Fix luồng tham gia, routing tới View phòng ngay khi Join thành công.
- [SessionRoomPage.tsx](MezonQuizFE/src/Pages/SessionRoomPage.tsx) — Logic phân role Host/Participant (isHost) hiển thị UI Quiz Player và gắn kết nối API Real-time.

**Verification**
1. Mở User A (Host) tạo phòng và tham gia -> Hiển thị Dashboard quản lý Session.
2. Mở User B & C (Participant) nhập ID phòng vào page Find Quiz -> Vào Mode chờ Host.
3. Host bấm Start -> Console và UI của User B & C thay đổi sang hiện câu hỏi thứ 1 ngay lập tức nhờ vào luồng đẩy SignalR.
4. Participant chọn câu trả lời -> Báo Toast đáp án đúng/sai, check dữ liệu database xem điểm có tăng tương ứng hay không.

**Decisions**
- Loại bỏ cơ chế Polling cũ (`setInterval` load mỗi 5 giây) để bảo vệ hiệu năng cho server và thay bằng Push Data của SignalR - phù hợp nhất với mô hình thi trắc nghiệm Cùng Lúc (Live Quiz).
- Tái sử dụng `SessionRoomPage.tsx` thay vì tạo route Controller riêng biệt bằng việc flag `isHost` để render 2 Interface khác nhau, giúp đồng nhất 1 URL `session/:id` quản lý thuận tiện state cho cả hai.