# ECommerceAPI

ECommerceAPI, bir e-ticaret platformu için mikro hizmet mimarisi kullanılarak geliştirilmiş bir API projesidir. Bu proje, kullanıcı kimlik doğrulama, ürün yönetimi, sipariş yönetimi ve API Gateway hizmetlerini içermektedir.

## Proje Yapısı

Proje dört ana mikro hizmetten oluşmaktadır:

1. **AuthService**: Kullanıcı kimlik doğrulama ve yetkilendirme hizmeti.
2. **ProductService**: Ürün yönetimi hizmeti.
3. **OrderService**: Sipariş yönetimi hizmeti.
4. **GatewayService**: API Gateway hizmeti.

## Başlarken

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

### Kurulum

1. Bu projeyi klonlayın:

    ```sh
    git clone https://github.com/efekantgn/ECommerceAPI.git
    cd ECommerceAPI
    ```

2. Veritabanı bağlantı dizelerini `appsettings.json` dosyalarında güncelleyin.

3. Veritabanı migrasyonlarını uygulayın:

    ```sh
    dotnet ef database update -p AuthService/AuthService.csproj -s AuthService/AuthService.csproj
    dotnet ef database update -p ProductService/ProductService.csproj -s ProductService/ProductService.csproj
    dotnet ef database update -p OrderService/OrderService.csproj -s OrderService/OrderService.csproj
    ```

4. Projeyi çalıştırın:

    ```sh
    dotnet run --project GatewayService/GatewayService.csproj
    ```

### Kullanım

#### AuthService

- **Kullanıcı Kaydı**: `POST /auth/register`
- **Kullanıcı Girişi**: `POST /auth/login`
- **Şifre Sıfırlama**: `POST /auth/reset-password`
- **Admin Bilgisi**: `GET /auth/admin`

#### ProductService

- **Ürünleri Listeleme**: `GET /product`
- **Ürün Listeleme**: `GET /product/{id}`
- **Ürün Ekleme**: `POST /product`
- **Ürün Güncelleme**: `PUT /product/{id}`
- **Ürün Silme**: `DELETE /product/{id}`
- **Ürün İnceleme Ekleme**: `POST /product/{productId}/reviews`
- **Ürün İncelemeleri Getirme**: `GET /product/{productId}/reviews`

#### OrderService

- **Sipariş Oluşturma**: `POST /order`
- **Sipariş Güncelleme**: `PUT /order/{id}`
- **Sipariş İptali**: `POST /order/{id}/cancel`
- **Sipariş İadesi**: `POST /order/{id}/return`

### Katkıda Bulunma

Katkıda bulunmak isterseniz, lütfen aşağıdaki adımları izleyin:

1. Bu projeyi forklayın.
2. Yeni bir dal oluşturun (`git checkout -b feature/ozellik`).
3. Değişikliklerinizi commit edin (`git commit -am 'Yeni özellik ekle'`).
4. Dalınıza push edin (`git push origin feature/ozellik`).
5. Bir Pull Request oluşturun.

### Lisans

Bu proje MIT Lisansı ile lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakın.

### İletişim

Herhangi bir sorunuz veya geri bildiriminiz varsa, lütfen [email@example.com](mailto:email@example.com) adresinden benimle iletişime geçin.