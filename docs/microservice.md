# Nền tảng Microservices

> Đây là file tổng quan về **Microservice**. Mục tiêu: sau khi đọc xong phải hiểu được
> *vì sao* hệ thống được chia nhỏ, các service *nói chuyện* với nhau ra sao, và
> *cost* phải trả khi chọn microservices. Mọi pattern phức tạp ở các file
> sau (`patterns.md`) đều là hệ quả trực tiếp của những nguyên lý ở đây.

---

## 1. Từ Monolith đến Microservices

### 1.1 Monolith là gì

Một ứng dụng duy nhất, mọi thứ (auth, product, order, payment) nằm trong cùng
một codebase, deploy thành một khối, dùng chung một database.

```
        ┌─────────────────────────────────────┐
        │             MONOLITH                │
        │ ┌──────┐ ┌───────┐ ┌──────┐ ┌─────┐ │
        │ │ Auth │ │Product│ │Order │ │ Pay │ │   Tất cả gọi nhau bằng function call trong bộ nhớ
        │ └──────┘ └───────┘ └──────┘ └─────┘ │   
        │           (cùng tiến trình)         │   
        └──────────────────┬──────────────────┘
                           ▼
                  ┌──────────────────┐
                  │  1 database      │   Mọi bảng chung một chỗ
                  └──────────────────┘
```

Điểm cốt lõi để hiểu monolith: **mọi lời gọi giữa các module là function call trong cùng một tiến trình.** Khi `OrderService.create()` gọi `ProductService.getPrice()`, đó chỉ là một lệnh nhảy trong bộ nhớ - mất vài nanosecond, không bao giờ "thất bại giữa đường", không có độ trễ mạng, không có chuyện trả lời hai lần. Đây là thứ sẽ *mất* khi tách ra microservices, và là gốc rễ của gần như mọi khó khăn về sau.

Monolith **không xấu**. Với đa số dự án nhỏ/vừa thì đây sẽ là lựa chọn đúng:

- **Đơn giản để phát triển:** một project, một lần `run`, một IDE mở hết được.
- **Dễ debug:** một stack trace duy nhất dẫn thẳng tới dòng code gây lỗi.
- **Nhất quán dữ liệu miễn phí:** một transaction SQL (`BEGIN ... COMMIT`) đảm bảo hoặc tất cả thay đổi cùng xảy ra, hoặc không cái nào - tính chất ACID.
- **Deploy một phát là xong:** không cần điều phối nhiều dịch vụ.

**Martin Fowler** có một lời khuyên nổi tiếng gọi là *"Monolith First"*: gần như mọi hệ thống lớn thành công đều khởi đầu từ một monolith rồi mới tách dần. Lý do là khi mới bắt đầu, ta chưa hiểu rõ domain - chưa biết đường ranh giới giữa các service nên nằm ở đâu. Vẽ sai ranh giới ngay từ đầu còn tệ hơn là không tách.

### 1.2 Microservices là gì

Tách ứng dụng thành nhiều service nhỏ, mỗi service:

- Chạy như một tiến trình/đơn vị deploy **riêng**.
- Sở hữu **database riêng** (không service nào đụng DB của service khác).
- Giao tiếp với nhau qua **mạng** (HTTP/gRPC) hoặc **message broker** (Kafka).
- Được tổ chức quanh một **năng lực nghiệp vụ** (business capability) - ví dụ "quản lý sản phẩm", "xử lý thanh toán" - chứ không phải quanh một tầng kỹ thuật.

```
                                  ┌──────────┐
                                  │  Client  │
                                  └────┬─────┘
                                       │ Gọi qua mạng, không phải function call
                                       ▼
                                ┌─────────────┐
                                │ API Gateway │
                                └──────┬──────┘
                                       │
      ┌────────────────┬───────────────┼─────────────────┬─────────────────┐
      │                │               │                 │                 │
      ▼                ▼               ▼                 ▼                 ▼
┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐
│    Auth    │   │  Product   │   │ Inventory  │   │   Order    │   │    Pay     │
│  Service   │   │  Service   │   │  Service   │   │  Service   │   │  Service   │
└─────┬──────┘   └─────┬──────┘   └─────┬──────┘   └─────┬──────┘   └─────┬──────┘
      │                │                │                │                │
      ▼                ▼                ▼                ▼                ▼
┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐
│  auth_db   │   │ product_db │   │ inventory  │   │  order_db  │   │   pay_db   │
└────────────┘   └────────────┘   └────────────┘   └────────────┘   └────────────┘
```

Trong dự án này: Auth, Product, Cart, Search... mỗi cái một tiến trình, một DB riêng, nhưng **cùng một ngôn ngữ - C# / .NET 10**. Đây là lựa chọn có chủ đích:một stack duy nhất để đi sâu vào kiến trúc và pattern, thay vì trải mỏng qua nhiều ngôn ngữ. Tính độc lập của service đến từ **ranh giới triển khai + DB riêng + giao tiếp qua API/event**, chứ không phải từ việc khác ngôn ngữ. Mỗi service vẫn tự do chọn kiểu lưu trữ hợp bài toán (Postgres, Redis, Elasticsearch) - tức **polyglot ở tầng lưu trữ**, không polyglot ở tầng ngôn ngữ.

> **Một service "nhỏ" cỡ nào?** Không có con số dòng code chuẩn. Thước đo thực dụng
> là: một service nên đủ nhỏ để một nhóm nhỏ (2-4 người) sở hữu trọn vẹn, hiểu hết,
> và viết lại trong vài tuần nếu cần. Nếu không thể giải thích trách nhiệm của
> một service trong một câu chứng tỏ nó đang ôm quá nhiều việc.

### 1.3 Vì sao tách? (Lợi ích của Microservices)

- **Scale độc lập:** Product bị đọc nhiều -> nhân bản riêng Product (chạy 10 bản song song), không cần nhân cả hệ thống. Trong monolith, muốn scale phần đọc sản phẩm bạn phải nhân bản *toàn bộ* ứng dụng, lãng phí tài nguyên.
- **Deploy độc lập:** sửa Notification không phải build lại Order. Mỗi service có vòng đời release riêng - đây là lợi ích lớn nhất về mặt tốc độ phát triển.
- **Cô lập lỗi (fault isolation):** Payment sập không nhất thiết kéo sập Product (nếu thiết kế đúng, có circuit breaker, fallback...). Trong monolith, một memory leak ở module thanh toán có thể làm sập cả tiến trình, kéo theo các service khác cũng chết chung dùn không có lỗi gì.
- **Tự do công nghệ (polyglot):** về lý thuyết mỗi service có thể chọn ngôn ngữ/store riêng hợp việc. Dự án này **cố tình không** dùng đa ngôn ngữ - chọn thuần .NET 10 để học sâu một hệ sinh thái - nhưng vẫn giữ polyglot ở tầng lưu trữ (Postgres, Redis, Elasticsearch tùy service).
- **Team scale:** nhiều nhóm làm song song trên các service khác nhau mà ít giẫm chân nhau. Đây thực ra là động lực *tổ chức* - theo Conway's Law, cấu trúc hệ thống có xu hướng phản chiếu cấu trúc giao tiếp của các nhóm xây nó.

### 1.4 Cái giá phải trả (tác hại của microservices)

Microservices **đổi sự đơn giản lấy khả năng mở rộng**. Chúng ta sẽ nhận về một loạt vấn đề mà monolith không có:

| Vấn đề | Monolith | Microservices |
|---|---|---|
| Gọi hàm | Trong bộ nhớ, nhanh, tin cậy | Qua mạng - chậm, có thể fail |
| Transaction | 1 transaction SQL lo hết | Phân tán nhiều DB - không có transaction chung |
| Nhất quán dữ liệu | Mạnh, tức thì | Thường chỉ "eventual" (nhất quán dần) |
| Debug | 1 stack trace | Lần theo nhiều service, cần distributed tracing |
| Triển khai | 1 unit | Nhiều unit, cần orchestration (K8s) |
| Lỗi cục bộ | Hiếm | Mạng chập chờn là chuyện thường |
| Kiểm thử | Unit + integration đơn giản | Phải test cả contract giữa service |
| Vận hành | 1 log, 1 metric | Log/metric phân tán, cần tập trung hóa |

Có một hệ quả tinh tế ít người nói tới: **độ phức tạp không biến mất, nó chỉ di chuyển.** Trong monolith, độ phức tạp nằm trong code (các module rối nhau). Trong microservices, code mỗi service sạch hơn, nhưng độ phức tạp dồn vào *khoảng giữa* - mạng, hạ tầng, điều phối, giám sát. Chúng ta cần một đội ngũ vận hành (hoặc kỹ năng DevOps) tốt hơn hẳn để chạy microservices.

> **Câu thần chú:** "Mạng là không đáng tin." Mọi lời gọi service-to-service đều có
> thể chậm, mất gói, hoặc trả lời hai lần. Toàn bộ các pattern ở `patterns.md`
> sinh ra để sống chung với sự thật này.

Mở rộng câu thần chú này thành cái thường gọi là **"8 ngộ nhận về hệ phân tán"**(Fallacies of Distributed Computing) - những giả định sai mà lập trình viên hay mắc khi mới chuyển từ monolith:

1. Mạng luôn ổn định. (Sai - cáp đứt, switch lỗi.)
2. Độ trễ bằng không. (Sai - mỗi hop mạng tốn mili-giây.)
3. Băng thông vô hạn. (Sai.)
4. Mạng an toàn. (Sai - cần auth giữa các service.)
5. Sơ đồ mạng không đổi. (Sai - IP, container lên xuống liên tục.)
6. Có một quản trị viên duy nhất. (Sai.)
7. Chi phí vận chuyển bằng không. (Sai - serialize/deserialize tốn CPU.)
8. Mạng đồng nhất. (Sai.)

Mỗi pattern bạn học sau này (retry, timeout, circuit breaker, idempotency...) là một liều thuốc giải cho một trong các ngộ nhận trên.

### 1.5 Khi nào KHÔNG nên microservices

Khi chưa hiểu domain, team nhỏ, traffic thấp. Lúc đó monolith tốt hơn. Cụ thể, hãy ở lại monolith nếu:

- Chưa hiểu rõ ranh giới nghiệp vụ -> tách sai còn tệ hơn không tách.
- Đội ngũ nhỏ, chưa có năng lực vận hành phân tán (CI/CD, monitoring, K8s).
- Tải còn thấp, chưa có nút thắt scale nào thực sự.
- Cần nhất quán mạnh, tức thì trên nhiều thực thể nghiệp vụ.

Dự án này là **để tự học** - nên việc triển khai theo microservices là hợp lý, miễn là ý thức được rằng ngoài đời nhiều hệ thống nên bắt đầu từ monolith rồi mới tách. Một con đường trung gian phổ biến là **"modular monolith"**: một deploy duy nhất nhưng code chia thành các module ranh giới rõ ràng, để sau này tách ra service dễ dàng. Nhưng vì mục tiêu học, ta đi thẳng vào microservices.

---

## 2. Các service nói chuyện với nhau như thế nào

Có hai kiểu giao tiếp lớn. Hiểu rõ khác biệt này là chìa khóa của cả dự án.

### 2.1 Đồng bộ (synchronous) - "gọi và chờ"

Service A gọi service B qua HTTP/gRPC và **đứng chờ** B trả lời rồi mới đi tiếp.

```
  Gateway ──HTTP GET /products/123──▶ Product
          ◀─────── 200 + JSON ───────
          (Gateway CHỜ trong lúc Product xử lý)
```

- **Hợp khi:** cần kết quả ngay để tiếp tục (đọc dữ liệu, kiểm tra quyền).
- **Nhược:** A phụ thuộc B. B chậm -> A chậm. B sập -> A lỗi theo (gọi là *temporal coupling* - ràng buộc theo thời gian: cả hai phải sống cùng lúc).
- Trong dự án: Gateway -> các service, Cart -> Product (lấy giá hiện tại) là gọi đồng bộ.

**HTTP/REST hay gRPC?** Cả hai đều là giao tiếp đồng bộ kiểu request/response.

- **REST trên HTTP/JSON:** dễ đọc, dễ debug bằng `curl`/Postman, ai cũng hiểu, hợp cho API ra ngoài (public) và giao tiếp Gateway ↔ client.
- **gRPC:** dùng HTTP/2 + Protocol Buffers (nhị phân), nhanh hơn và nhẹ hơn nhiều, có hợp đồng (`.proto`) chặt chẽ sinh code tự động, hỗ trợ streaming. Hợp cho giao tiếp *nội bộ* service-to-service nơi hiệu năng quan trọng.

Một mối nguy điển hình của giao tiếp đồng bộ là **dây chuyền phụ thuộc**: nếu A gọi B, B gọi C, C gọi D, thì độ trễ cộng dồn và xác suất lỗi nhân lên. Chỉ cần D có độ sẵn sàng 99%, thì một chuỗi 4 service đã tụt xuống ~96%. Đây là lý do ta cố giữ chuỗi gọi đồng bộ càng ngắn càng tốt, và đẩy những gì có thể sang bất đồng bộ.

### 2.2 Bất đồng bộ (asynchronous) - "bắn rồi quên"

Service A **phát một sự kiện** (event) lên broker (Kafka) rồi đi tiếp ngay, không chờ. Các service quan tâm sẽ tự đọc và xử lý sau.

```
  Order ──"OrderCreated"──▶ [ Kafka ] ──▶ Inventory  (giữ hàng)
        (phát xong đi luôn,    │       ──▶ Payment    (thu tiền)
         KHÔNG chờ ai)         │       ──▶ Notification (báo user)
                               └──▶ (ai quan tâm thì nghe)
```

- **Hợp khi:** A không cần kết quả ngay; nhiều bên cùng quan tâm một sự việc.
- **Lợi:** A và B **tách rời** (decoupled). Payment sập tạm thời? Event vẫn nằm trong Kafka, Payment sống lại sẽ xử lý tiếp. Order không bị ảnh hưởng.
- **Nhược:** dữ liệu chỉ nhất quán *dần* (eventual consistency). Ngay sau khi đặt hàng, có thể tồn kho chưa kịp trừ trong vài trăm ms.
- Trong dự án: toàn bộ luồng Order -> Inventory -> Payment -> Notification chạy qua Kafka chính là async.

Có hai kiểu nhắn tin bất đồng bộ cần phân biệt:

- **Lệnh / hàng đợi điểm-tới-điểm (command/queue):** "Hãy gửi email này" - chỉ một consumer xử lý, mang nghĩa *ra lệnh*.
- **Sự kiện / phát-đăng-ký (event/pub-sub):** "OrderCreated đã xảy ra" - bất kỳ ai quan tâm đều nghe được, mang nghĩa *thông báo*. Kafka thiên về kiểu này.

Sự khác biệt không chỉ là kỹ thuật mà là *ý đồ*: lệnh nói cho một người nghe cụ thể phải làm gì; sự kiện chỉ kể lại một sự thật đã xảy ra và để người nghe tự quyết.

### 2.3 So sánh nhanh

```
ĐỒNG BỘ (HTTP/gRPC)              BẤT ĐỒNG BỘ (Kafka event)
──────────────────              ─────────────────────────
A hỏi -> chờ -> B đáp             A phát event -> đi tiếp luôn
Cần B sống cùng lúc             B có thể xử lý sau
Phù hợp: đọc, query             Phù hợp: phản ứng dây chuyền
Ràng buộc chặt                  Tách rời
Nhất quán tức thì               Nhất quán dần (eventual)
Lỗi lan ngay lập tức            Lỗi được hấp thụ bởi broker
Dễ suy luận, dễ debug          Khó lần vết hơn, cần tracing
```

> **Nguyên tắc thực dụng:** dùng đồng bộ khi *cần câu trả lời để làm tiếp ngay*;
> dùng bất đồng bộ khi *chỉ cần thông báo "việc X đã xảy ra"* cho người khác phản ứng.

### 2.4 Event-driven là gì

Kiến trúc nơi các service phản ứng với **sự kiện** thay vì gọi thẳng nhau. Thay vì Order ra lệnh "Inventory, trừ hàng đi!", Order chỉ thông báo "OrderCreated đã xảy ra" và Inventory tự quyết định phải làm gì. Đây là tinh thần xuyên suốt dự án của bạn.

Lợi ích lớn nhất là **inversion of dependency** (đảo ngược phụ thuộc): muốn thêm một service mới phản ứng với đơn hàng (ví dụ Analytics), bạn **không phải sửa Order**
- chỉ cần service mới đăng ký nghe event `OrderCreated`. Order không biết và không cần biết ai đang nghe. So sánh trực tiếp:

```
  Gọi thẳng (coupling chặt):           Event-driven (coupling lỏng):
  ────────────────────────             ──────────────────────────────
  Order ─▶ Inventory                   Order ─▶ "OrderCreated" ─▶ Kafka
  Order ─▶ Payment                                                 │
  Order ─▶ Notification                          Inventory ◀───────┤
  Order ─▶ Analytics  ← phải sửa Order            Payment ◀────────┤
          mỗi khi thêm                       Notification ◀────────┤
                                                Analytics ◀────────┘  ← chỉ cần
                                                                       đăng ký, không
                                                                       động vào Order
```

Cái giá của sự tách rời này: **Order không còn biết việc đã hoàn tất hay chưa.** Nó phát event rồi đi. Nếu Payment thất bại, Order không biết ngay - phải có cơ chế bù trừ (Saga) để xử lý. Đây là điểm bạn sẽ gặp lại ở Phase 5 & 7.

Một khái niệm thường đi kèm là **Choreography vs Orchestration** (sẽ đào sâu ở
`04-patterns.md`):

- **Choreography (vũ đạo):** mỗi service tự nghe event và phản ứng, không có "nhạc trưởng". Phi tập trung, lỏng lẻo, nhưng luồng tổng thể khó nhìn ra.
- **Orchestration (chỉ huy):** một service điều phối trung tâm ra lệnh từng bước. Dễ nhìn luồng, nhưng service điều phối trở thành điểm tập trung phụ thuộc.

---

## 3. Database per service - quy tắc bất khả xâm phạm

Mỗi service sở hữu DB riêng. Service khác **tuyệt đối không** query thẳng vào DB đó; muốn dữ liệu thì phải gọi API hoặc nghe event.

```
   ✘ SAI                              ✔ ĐÚNG
   ─────                              ─────
   Order ───query──▶ product_db       Order ──API call──▶ Product ──▶ product_db
        (đụng DB của Product)              (đi qua cửa chính của Product)
```

Việc nhiều service chia sẻ chung một DB được gọi tên thẳng là **Shared Database anti-pattern** (mẫu thiết kế *phản* tác dụng). Lý do nó nguy hiểm là vì nó tạo ra sự phụ thuộc ngầm: hai service trông như tách rời nhưng thực ra dính chặt qua cấu trúc bảng dùng chung.

### 3.1 Vì sao nghiêm ngặt vậy

- **Coupling qua schema:** Nếu Order query thẳng `product_db`, thì khi Product đổi schema bảng (đổi tên cột, tách bảng), Order vỡ - dù Order không hề được sửa. Hai service dính chặt vào nhau qua DB, mất hết lợi ích của việc tách. Tệ hơn, nhóm Product không thể biết ai đang phụ thuộc vào bảng của mình để mà cảnh báo.
- **Tự do chọn store:** DB riêng cho phép mỗi service chọn loại store hợp nhất: Product dùng Postgres (quan hệ, giao dịch), Search dùng Elasticsearch (tìm kiếm toàn văn), Cart dùng Redis (key-value, nhanh, TTL). Đây gọi là **polyglot persistence**. (Đúng như bảng store trong file của bạn.)
- **Cô lập sự cố & scale:** DB của Search bị quá tải vì truy vấn nặng không làm chậm DB của Order. Mỗi DB tune, backup, scale độc lập.
- **Ranh giới sở hữu rõ ràng:** chỉ một service được ghi vào một bảng -> không có chuyện hai service đua nhau sửa cùng một dòng theo logic khác nhau.

> **Lưu ý thực tế về triển khai:** "DB riêng" không nhất thiết là *một máy chủ
> Postgres riêng cho mỗi service*. Bạn có thể chạy một Postgres instance nhưng tạo
> nhiều **schema/logical database** tách biệt, mỗi service chỉ có credential truy
> cập đúng phần của nó. Cái bất biến là *ranh giới logic và quyền truy cập*, không
> phải số lượng máy chủ vật lý. Với dự án học, dùng chung một Postgres nhiều schema
> là hoàn toàn hợp lý.

### 3.2 Hệ quả: dữ liệu bị trùng lặp một cách có chủ đích

Search cần biết tên/giá sản phẩm, nhưng không được đụng `product_db`. Giải pháp: Product phát event `ProductUpdated`, Search nghe và **tự lưu một bản sao** trong Elasticsearch của mình. Đây gọi là **read-model** (mô hình đọc) - xem `04-patterns.md`.

```
  Product ──"ProductUpdated {id, tên, giá}"──▶ Kafka
                                                 │
                                                 ▼
                                            Search nghe được
                                                 │
                                                 ▼
                                        lưu bản sao tên+giá vào
                                        Elasticsearch của riêng nó
                                                 │
                                                 ▼
                                    giờ Search tự trả lời truy vấn
                                    mà KHÔNG cần hỏi Product nữa
```

> Trùng lặp dữ liệu trong microservices là **bình thường và đúng**, không phải lỗi
> thiết kế. Đổi lại là tính độc lập. Cái giá là phải đồng bộ qua event.

Điều đáng nói: bản sao này luôn **trễ một chút** so với bản gốc (vì event mất thời gian truyền và xử lý). Đó là một biểu hiện cụ thể của eventual consistency ở mục tiếp theo. Bạn chấp nhận rằng giá hiển thị trong kết quả tìm kiếm có thể "cũ" vài trăm mili-giây so với giá thật trong `product_db` - và với chức năng search thì điều đó hoàn toàn ổn.

### 3.3 Vậy làm sao query dữ liệu nằm ở nhiều service?

Đây là câu hỏi nhức nhối nhất của database-per-service. Ví dụ: "lấy đơn hàng kèm tên sản phẩm và tên khách" - dữ liệu nằm ở 3 service. Không còn `JOIN` SQL được nữa. Hai hướng giải quyết chính (chi tiết ở `04-patterns.md`):

- **API Composition:** một thành phần (thường là Gateway hoặc một service tổng hợp) gọi lần lượt từng service rồi ghép kết quả trong bộ nhớ. Đơn giản, nhưng kém hiệu quả nếu phải ghép nhiều và lọc phức tạp.
- **CQRS (Command Query Responsibility Segregation):** dựng sẵn một read-model tổng hợp (như mục 3.2) bằng cách nghe event từ nhiều service, để truy vấn đọc nhanh. Phức tạp hơn nhưng đọc rất nhanh.

---

## 4. CAP và Eventual Consistency (hiểu ở mức trực giác)

Bạn sẽ nghe nhiều về "eventual consistency". Hiểu đơn giản:

Trong hệ phân tán, khi dữ liệu nằm rải ở nhiều DB, bạn **không thể** vừa luôn chính xác tức thì, vừa luôn sẵn sàng, vừa chịu được lỗi mạng - cùng lúc. Phải đánh đổi.

### 4.1 Định lý CAP nói gì

CAP là viết tắt của ba tính chất:

- **C - Consistency (nhất quán):** mọi lần đọc đều thấy dữ liệu mới nhất, không bao giờ thấy dữ liệu cũ. (Lưu ý: chữ "C" này khác với chữ C trong ACID.)
- **A - Availability (sẵn sàng):** mọi yêu cầu đều nhận được phản hồi (không lỗi), dù phản hồi đó có thể dựa trên dữ liệu hơi cũ.
- **P - Partition tolerance (chịu phân mảnh mạng):** hệ vẫn chạy được khi mạng giữa các node bị đứt/chậm.

Định lý CAP nói: khi xảy ra phân mảnh mạng (P) - điều **chắc chắn sẽ xảy ra** trong hệ phân tán thật - bạn buộc phải chọn giữa C và A. Không thể có cả hai. Vì P là bắt buộc trong thực tế, lựa chọn thực sự của bạn là: **khi mạng đứt, ưu tiên nhất quán (từ chối phục vụ để khỏi trả dữ liệu sai) hay ưu tiên sẵn sàng (vẫn phục vụ, chấp nhận dữ liệu có thể tạm cũ)?**

Đa số hệ microservices thiên về **AP**: ưu tiên luôn sẵn sàng, chấp nhận nhất quán dần. Lý do đơn giản: với phần lớn nghiệp vụ thương mại điện tử, để khách thấy giá trễ 300ms thì chấp nhận được, nhưng để cả trang sập thì mất tiền thật.

### 4.2 Eventual consistency trong luồng đặt hàng

Ví dụ trong dự án: bạn đặt hàng xong, màn hình báo "đặt thành công" ngay. Nhưng việc trừ kho, thu tiền, gửi mail diễn ra *vài giây sau* qua các event Kafka. Trong khoảnh khắc đó, dữ liệu chưa nhất quán hoàn toàn - và **điều đó chấp nhận được**, miễn là cuối cùng nó hội tụ về đúng.

```
  t=0ms   User bấm "Đặt hàng" -> Order lưu đơn, phát OrderCreated -> báo OK ngay
  t=200ms Inventory nghe được -> trừ kho
  t=400ms Payment nghe được   -> thu tiền
  t=600ms Notification        -> gửi email
          ▲
          └── giữa chừng, "trạng thái toàn hệ thống" chưa nhất quán - nhưng sẽ hội tụ
```

"Eventual" (nhất quán *dần*) nghĩa là: **nếu ngừng có cập nhật mới, thì sau một khoảng thời gian hữu hạn, mọi bản sao dữ liệu sẽ hội tụ về cùng một giá trị đúng.** Nó không hứa "ngay lập tức", chỉ hứa "rồi sẽ đúng". Thiết kế hệ thống tốt là làm cho khoảng "chưa đúng" đó đủ ngắn và đủ vô hại để người dùng không bận tâm - ví dụ hiển thị trạng thái đơn là "đang xử lý" thay vì khẳng định chắc nịch mọi thứ đã xong.

### 4.3 Khi nào cần nhất quán mạnh, và cái giá của nó

Nếu bạn cần nhất quán *tức thì tuyệt đối* xuyên nhiều service (hiếm), bạn phải dùng các pattern phức tạp như **Saga** (xem `04-patterns.md`) - một chuỗi các giao dịch cục bộ, mỗi bước phát event kích hoạt bước sau, và nếu một bước thất bại thì chạy các **giao dịch bù trừ** (compensating transaction) để hoàn tác những gì đã làm. Saga không cho bạn nhất quán *tức thì*, nhưng cho bạn sự nhất quán *cuối
cùng có kiểm soát* kèm khả năng tự dọn dẹp khi lỗi. Đó chính là lý do Phase 5 & 7
của bạn tồn tại.

Hai khái niệm bạn nên ghi nhớ vì sẽ gặp lại liên tục khi làm việc với event:

- **Idempotency (lũy đẳng):** xử lý cùng một event hai lần phải cho kết quả y như xử lý một lần. Vì Kafka đảm bảo "ít nhất một lần" (at-least-once), một event có thể đến hai lần - code consumer phải chịu được điều đó (ví dụ kiểm tra "đơn này đã trừ kho chưa" trước khi trừ).
- **Thứ tự (ordering):** trong một partition Kafka, event giữ đúng thứ tự; giữa các partition thì không. Chọn khóa phân vùng (ví dụ theo `orderId`) để các event liên quan tới cùng một đơn luôn vào cùng một partition và được xử lý đúng thứ tự.

---

## 5. Bức tranh tổng thể của dự án bạn

Ghép mọi thứ trên lại, đây là cách đọc sơ đồ kiến trúc trong file của bạn:

```
  Client -> Gateway (YARP, .NET)
              │  gọi ĐỒNG BỘ (HTTP public / gRPC nội bộ) để đọc/ghi trực tiếp
              ├─────────────▶ Product (.NET) ⇄ product_db (Postgres)
              ├─────────────▶ Auth (.NET)    ⇄ auth_db
              ├─────────────▶ Cart (.NET)    ⇄ Redis
              └─────────────▶ ...

  Khi có việc cần phản ứng dây chuyền -> đi BẤT ĐỒNG BỘ qua Kafka:

      Order ──OrderCreated──▶ Kafka ──▶ Inventory ──StockReserved──▶ Kafka
                                                                       │
                              Payment ◀──────────────────────────────┘
                                 │
                                 └─PaymentCompleted─▶ Kafka ─▶ Notification ─▶ (WebSocket) ─▶ Client

  Redis: cache + rate-limit ở Gateway, lưu giỏ ở Cart, pub/sub đẩy WebSocket.
```

Để ý hai "phong cách" giao tiếp cùng tồn tại, mỗi cái cho một mục đích:

- **Trục đồng bộ (HTTP qua Gateway):** dành cho việc *đọc và ghi cần kết quả ngay* - xem sản phẩm, đăng nhập, thêm vào giỏ. Client cần câu trả lời tức thì.
- **Trục bất đồng bộ (Kafka):** dành cho *phản ứng dây chuyền sau một sự kiện* - đặt hàng kéo theo trừ kho, thu tiền, gửi mail. Không ai cần chờ ai.

Đây gọi là kiến trúc lai (hybrid), và nó là chuẩn mực thực tế: gần như không hệ microservices nghiêm túc nào thuần đồng bộ hoặc thuần bất đồng bộ.

Ba mảnh hạ tầng bạn vừa dựng ở Phase -1 ánh xạ thẳng vào đây:

- **Postgres** = nơi mỗi service .NET lưu dữ liệu nghiệp vụ của nó (mỗi service một schema/DB riêng - quy tắc mục 3).
- **Redis** = cache (giảm tải đọc), rate-limit (chặn lạm dụng ở Gateway), giỏ hàng (key-value có TTL), pub/sub (đẩy realtime ra WebSocket).
- **Kafka** = xương sống event-driven nối mọi service phản ứng với nhau; đồng thời là bộ đệm hấp thụ sự cố (service nghe sập tạm thời thì event vẫn nằm chờ trong topic, không mất).

Vì sao Kafka mà không phải "Order gọi HTTP thẳng tới Inventory/Payment/Notification"? Vì nếu gọi thẳng đồng bộ: (1) Order phải biết địa chỉ cả ba và chờ cả ba xong - chậm và mong manh; (2) một service sập là cả luồng đặt hàng sập; (3) thêm Analytics phải sửa code Order. Kafka gỡ cả ba vấn đề: Order chỉ phát một event và quên đi, broker lưu trữ và phân phát, ai sập thì xử lý bù khi sống lại, ai mới thì tự đăng ký nghe.

Đọc tiếp ba file `01`, `02`, `03` để hiểu từng mảnh hạ tầng (Postgres, Redis, Kafka), rồi `04-patterns.md` để xem các pattern (Saga, CQRS, Outbox, Circuit Breaker...) ghép chúng lại thành một hệ thống chịu lỗi ra sao.