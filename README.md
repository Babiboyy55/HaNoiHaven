# 🏠 HanoiHaven

**HanoiHaven** là nền tảng tìm kiếm và cho thuê phòng trọ tại Hà Nội, được xây dựng theo kiến trúc **ASP.NET Core MVC** chuẩn với giao diện hiện đại sử dụng **Tailwind CSS**.

---

## ✨ Tính năng

- **🔍 Tìm kiếm thông minh** — Tìm kiếm phòng theo tên hoặc khu vực qua thanh tìm kiếm trên Header.
- **🎛️ Bộ lọc đa tiêu chí** — Lọc kết quả theo:
  - Khoảng giá (VNĐ/tháng)
  - Loại phòng: Studio, Phòng chung, Nguyên căn
  - Tiện nghi: Điều hòa, WiFi tốc độ cao, Chỗ để xe, Bếp riêng
  - Khoảng cách đến trung tâm
- **🃏 Partial Views** — Card phòng trọ tái sử dụng được tách thành `_RoomCard.cshtml`.
- **⚡ Async/Await** — Toàn bộ luồng dữ liệu được xử lý bất đồng bộ, sẵn sàng cho kết nối Database.
- **💉 Dependency Injection** — `RoomService` được inject qua constructor, đảm bảo tính testable và decoupled.

---

## 🏗️ Kiến trúc dự án

```
nhatro/
├── Controllers/
│   └── HomeController.cs       # Nhận request, gọi Service, trả View
├── Models/
│   ├── AppDbContext.cs          # Entity Framework DbContext
│   ├── RoomListing.cs          # Model phòng trọ & tiện nghi
│   └── ErrorViewModel.cs
├── Services/
│   ├── IRoomService.cs          # Interface định nghĩa contract
│   └── RoomService.cs          # Logic nghiệp vụ & lọc dữ liệu
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml         # Trang chủ
│   │   └── Explore.cshtml       # Trang tìm kiếm & bộ lọc
│   └── Shared/
│       ├── _Layout.cshtml       # Layout chung (Header + Footer)
│       ├── _RoomCard.cshtml     # Partial view: Card phòng trọ
│       └── Error.cshtml
├── wwwroot/                     # Static files (CSS, JS)
├── Program.cs                   # Cấu hình DI và middleware
└── appsettings.json             # Connection string & cấu hình
```

---

## 🔄 Luồng hoạt động (MVC Flow)

```
User Request (URL/Form)
        ↓
  HomeController
  ├── Nhận tham số: query, minPrice, maxPrice, roomTypes[], amenities[], distance
  ├── Gọi IRoomService.GetFeaturedRoomsAsync(...)
  └── Trả ViewBag state + Model cho View
        ↓
   RoomService
  ├── Lấy danh sách phòng (mock / DB)
  └── Áp dụng filter LINQ theo từng tiêu chí
        ↓
   Explore.cshtml
  ├── Sidebar: <form> bộ lọc (giữ nguyên trạng thái qua ViewBag)
  └── Grid: @foreach → <partial name="_RoomCard" />
```

---

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | SQL Server |
| UI Styling | Tailwind CSS (CDN) |
| Icons | Google Material Symbols |
| Font | Google Fonts — Inter |

---

## 🚀 Chạy dự án

### Yêu cầu
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local hoặc Docker)

### Các bước

**1. Clone repository**
```bash
git clone <repository-url>
cd nhatro
```

**2. Cập nhật Connection String** trong `nhatro/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=RealEstateDB;Trusted_Connection=True;"
  }
}
```

**3. Chạy Migration (nếu có)**
```bash
cd nhatro
dotnet ef database update
```

**4. Chạy ứng dụng**
```bash
dotnet watch run
```

Ứng dụng sẽ chạy tại: `http://localhost:5043`

---

## 📋 Các trang hiện có

| Route | Mô tả |
|---|---|
| `/` | Trang chủ |
| `/Home/Explore` | Trang tìm kiếm & lọc phòng |
| `/Home/Explore?query=Tây Hồ` | Tìm kiếm theo từ khóa |
| `/Home/Explore?minPrice=3000000&maxPrice=10000000` | Lọc theo khoảng giá |
| `/Home/Explore?roomTypes=Studio&amenities=Điều hòa` | Lọc theo loại phòng + tiện nghi |

---

## 🗺️ Roadmap

- [ ] Kết nối Entity Framework với SQL Server thật
- [ ] Trang chi tiết phòng (`/Home/Detail/{id}`)
- [ ] Hệ thống xác thực người dùng (ASP.NET Identity)
- [ ] Chức năng "Yêu thích phòng" (Wishlist)
- [ ] Đăng tin cho thuê
- [ ] Phân trang thật (hiện đang là UI tĩnh)

---

## 👤 Tác giả

Dự án **HanoiHaven** — Nền tảng thuê phòng uy tín tại Hà Nội.
