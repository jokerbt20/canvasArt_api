# CanvasArt API

Production-ready REST API for an online art gallery and painting store.
ASP.NET Core 8 · **Dapper only** (no EF, hand-written SQL) · SQL Server · JWT + refresh tokens · FluentValidation · AutoMapper · Serilog · Swagger · ImageSharp.

## Solution layout (Clean Architecture)

```
CanvasArt.slnx
Directory.Build.props
Database/CreateDatabase.sql        <- schema + constraints + indexes + seed
src/
  CanvasArt.Domain/                <- entities, enums (no dependencies)
  CanvasArt.Application/           <- DTOs, interfaces, validators, mapping, services, pricing engine
  CanvasArt.Infrastructure/        <- Dapper repositories, JWT, BCrypt, ImageSharp, connection factory
  CanvasArt.API/                   <- controllers, middleware, filters, Program.cs, Swagger
```

Dependency direction: `API → Infrastructure → Application → Domain`.

## Getting started

1. **Create the database.** Run `Database/CreateDatabase.sql` against your SQL Server
   (SSMS, `sqlcmd`, or Azure Data Studio). It is idempotent and seeds roles, the
   administrator, example categories/tags and CMS settings.

2. **Configure** `src/CanvasArt.API/appsettings.json`:
   - `ConnectionStrings:DefaultConnection` — your SQL Server connection string.
   - `Jwt:SecretKey` — **replace** with a random secret of at least 32 characters.
   - `Cors:AllowedOrigins` — add your frontend origin(s); empty means allow-any.

3. **Run.**
   ```bash
   dotnet run --project src/CanvasArt.API
   ```
   Swagger UI: `https://localhost:7153/swagger` (or `http://localhost:5167/swagger`).

## Default administrator

| Email | Password |
|-------|----------|
| `admin@canvasarts.mk` | `Admin@Canvas2026` |

Change the password after first login (`POST /api/auth/change-password`).
Administrators can create more administrators via `POST /api/auth/users`.

## Standard response envelope

Every endpoint returns:

```json
{ "success": true, "message": "...", "data": { }, "errors": null }
```

Errors are produced by global exception handling with the correct HTTP status
(400 validation, 401/403 auth, 404 not found, 409 conflict, 500 unexpected).

## Key endpoints

| Area | Route | Access |
|------|-------|--------|
| Auth | `POST /api/auth/{register,login,refresh,revoke}`, `GET /api/auth/me` | public / bearer |
| Users | `POST /api/auth/users`, `GET /api/auth/users` | Administrator |
| Categories | `GET /api/categories`, `…/manage`, CRUD | public read / admin write |
| Tags | `GET /api/tags`, CRUD | public read / admin write |
| Paintings | `GET /api/paintings`, `GET /api/paintings/{slug}`, `…/manage`, CRUD, image upload | public read / admin write |
| Frames | `GET /api/frames`, `GET /api/frames/{id}`, CRUD, image | public read / admin write |
| Promotions | `/api/promotions` and `/api/promotions/combinations` | Administrator |
| Cart | `POST /api/cart/calculate` | public |
| Orders | `POST /api/orders`, `GET /api/orders/track/{number}`, admin list/detail/status/stats | mixed |
| CMS | `/api/slides`, `/api/settings`, `/api/home` | public read / admin write |

## Images

Uploads are validated (type + size), then the **original is stored privately**
under `Images:PrivateStorageRoot` (never served). Resized and watermarked variants
are written to `Images:PublicStorageRoot/images/{filename}.jpg` and served
read-only at `/uploads/images/{filename}.jpg`; thumbnails are written to
`Images:PublicStorageRoot/thumbs/{filename}.jpg` and served at
`/uploads/thumbs/{filename}.jpg`. Filenames are timestamp+GUID and globally
unique, so all entities (paintings, frames, slides) share the same flat
`images`/`thumbs` folders — no per-entity subfolder. Listings return
thumbnails; painting detail returns the watermarked image. Stored paths live
in `PaintingImages.ThumbnailPath`/`WatermarkPath`/`ResizedPath`/`OriginalPath`,
`Frames.ImagePath`/`ThumbnailPath`, and `Slides.ImagePath` (see
`Database/CreateDatabase.sql`).

Both storage roots may be absolute paths or relative to the app's content root.
In production (`appsettings.Production.json`) they're set to `../uploads` and
`../storage/private` so uploaded files live outside the deployed app folder
(e.g. sibling of `httpdocs` in a Plesk layout) and survive redeploys — adjust the
`../` depth if the app is deployed into a subfolder of the web root.

## Pricing & promotions

`PromotionEvaluator` loads the currently-active single and combination promotions
once per request and prices paintings, frames and painting+frame bundles
consistently across listings, cart preview and order creation. Orders are written
in a single transaction that also decrements stock atomically (oversell → 409).

## Notes

- Package advisory: `AutoMapper 13.0.1` and `SixLabors.ImageSharp` carry NuGet
  advisories at build time; both are the current stable lines used as required.
- All database access is hand-written SQL via Dapper with parameterized queries,
  `OFFSET/FETCH` pagination, `QueryMultiple` aggregates and explicit column lists.
