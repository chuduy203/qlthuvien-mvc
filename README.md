# qlthuvien-mvc
# Hướng dẫn cài đặt chương trình Quản lý Thư viện

## 1. Công nghệ sử dụng
- ASP.NET Core MVC .NET 8
- Entity Framework Core
- SQL Server
- Bootstrap 5
- jQuery AJAX
- TinyMCE editor
- Choices.js (select có tìm kiếm)

## 2. Cài đặt database (one-shot)
Chỉ cần chạy **1 file duy nhất**:

```text
Database/QLThuVien.sql
```

Script này sẽ:
- Xóa DB `QLThuVien` cũ (nếu có) và tạo mới.
- Seed dữ liệu mẫu quan trọng.

Tài khoản quản trị seed sẵn:
- Email: `admin@gmail.com`
- Mật khẩu: `123456`

## 3. Cấu hình connection string
Mở file:

```text
LibraryManagement/appsettings.json
```

Cập nhật:

```json
"DefaultConnection": "Server=.;Database=QLThuVien;Trusted_Connection=True;TrustServerCertificate=True"
```

## 4. Chạy project
Mở terminal tại:

```text
LibraryManagement
```

Chạy:

```bash
dotnet restore
dotnet run
```

Mở URL được in trên terminal (ví dụ `https://localhost:57434`).

## 5. Checklist chức năng đã có
- Đăng nhập/đăng xuất bằng Cookie Authentication.
- Session + cookie RememberMe.
- Phân quyền đăng nhập vận hành: `Admin/Librarian` (vai trò `Reader` chỉ để mở rộng, chưa có cổng độc giả tự phục vụ).
- CRUD: sách, thể loại, tác giả, NXB, độc giả, nhân viên.
- Mượn sách, trả sách.
- Tính phạt:
  - Quá hạn: `5000đ/ngày`
  - Rách/hỏng: `30% giá sách`
  - Mất: `100% giá sách`
- API REST: `GET /api/books`, `GET /api/books/{id}`.
- AJAX tìm kiếm sách.
- Upload ảnh bìa, TinyMCE editor.
- Validation bằng DataAnnotations.
- Responsive UI (Bootstrap).
- Đổi ngôn ngữ vi-VN / en-US trên hầu hết màn hình.

## 6. Phạm vi người dùng hiện tại
- Đây là hệ thống tác nghiệp nội bộ thư viện.
- Người thao tác trực tiếp trên phần mềm: `Admin` và `Thủ thư`.
- `Độc giả` hiện chỉ được quản lý dưới dạng dữ liệu nghiệp vụ (hồ sơ, lịch sử mượn/trả), chưa có màn hình tự đăng ký/tự đăng nhập/tự tra cứu riêng.

