# Product / Catalog Service (.NET 10)

Quản lý product, category, brand, variant, image. CRUD + list/search/filter/sort.
**Không** giữ tồn kho thật (tồn kho ở Inventory service). Giá lưu `decimal` → `NUMERIC(19,4)`.

## Cấu trúc
```
Domain/         EF entities + interface IAuditable, ISoftDeletable + enum ProductStatus
Data/           ProductDbContext (auto-stamp time + soft delete), Configurations/, Migrations/, ProductSeeder, DbContextFactory (design-time)
Dtos/           request/response records + ProductListQuery
Validation/     FluentValidation validators  [TODO: điền rule]
Mapping/        entity -> response (extension members)
Services/       business logic: ProductService, CategoryService, BrandService
Controllers/    ProductsController, CategoriesController, BrandsController  [TODO: điền action]
```
Cross-cutting (ApiResponse, error code, correlation-id, exception middleware) ở `shared/Ecommerce.BuildingBlocks`.

## Chạy local
```bash
make up                                   # Postgres/Redis/Kafka (ở repo root)
cd services/Product/Ecommerce.Product
dotnet user-secrets set "ConnectionStrings:ProductDb" "Host=localhost;Port=5432;Database=product_db;Username=ecom;Password=ecom_dev_password"
dotnet run                                # tự migrate; seed data mẫu chỉ chạy ở Development khi DB còn rỗng
```
- Swagger/OpenAPI (dev): `/openapi/v1.json`
- Health: `/health` (liveness), `/ready` (readiness — check Postgres)

## API (v1)
| Method | Route | Mô tả |
|---|---|---|
| GET | `/v1/products` | list: `?page=&pageSize=&categoryId=&brandId=&search=&sort=price|name|createdAt&order=asc|desc` |
| GET | `/v1/products/{id}` | chi tiết (kèm variants, images) |
| POST | `/v1/products` | tạo |
| PUT | `/v1/products/{id}` | cập nhật |
| DELETE | `/v1/products/{id}` | soft delete |
| ... | `/v1/categories`, `/v1/brands` | CRUD tương tự |

Mọi response bọc `ApiResponse` (`success`/`data`/`error`/`meta`). Lỗi dùng error code trong `contracts/errors`.

## Còn lại (TODO)
- [ ] `Program.cs`: hoàn tất wiring (Serilog, versioning, FluentValidation, health, middleware) — xem comment trong file.
- [ ] `Controllers/*`: điền action — xem comment trong từng file.
- [ ] `Validation/*`: điền rule — xem comment.
- [ ] Test (xUnit + Testcontainers).
- [ ] Xuất OpenAPI spec sang `contracts/openapi/product.yaml`.
- [ ] Thêm service `product` vào `infra/docker-compose.yml`.
