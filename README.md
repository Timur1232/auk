# Auk

Auk - это веб-платформа для проведения аукционов. Приложение позволяет
создавать создавать лоты, торговаться, делая ставки, и оформлять выкупы
товаров.

# Скриншоты

## Домашняя страница
![Домашняя страница](./docs/imgs/home_page.png)

## Страница лота
![Страница лота](./docs/imgs/lot_details.png)

## Лоты пользователя
![Лоты пользователя](./docs/imgs/this_user_lots.png)

## Ставки пользователя
![Ставки пользователя](./docs/imgs/user_bets.png)

## Покупки пользователя
![Покупки пользователя](./docs/imgs/purchases.png)

# Технологии

- [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) - основной фреймворк для API
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli) - ORM для работы с базой данных
- [JWT](https://www.jwt.io/introduction#what-is-json-web-token) - аутентификация пользователей
- [Docker](https://docs.docker.com/get-started/) - контейнеризация приложения
- [Docker Compose](https://docs.docker.com/compose/) - оркестрация контейнеров (сервер, бд)
- [HTMX](https://htmx.org/) - библиотека для построения пользовательского интерфейса

# Инструкция по запуску

## Предварительные требования
- Установите и настройте [.NET SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Установите и настройте [Docker](https://docs.docker.com/get-started/) и [Docker Compose](https://docs.docker.com/compose/)
- Если не используете Docker, то установите и настройте [PostgreSQL](https://www.postgresql.org/download/) для вашей системы

## Шаги запуска
Клонируйте репозиторий:
```bash
git clone https://github.com/Timur1232/auk.git
```

Настройте файл конфигурации бэкенда:
```bash
vim appsettings.json
```

Обновите следующие параметры:
- `ConnectionStrings.db_conn` - строка подключения к вашей БД
- `JwtSettings.Secret` - секретный ключ для JWT

### Обычный запуск
Убедитесь в работе PostgreSQL:
```
psql -h localhost -p 5432 -U postgres -d auk
```

Запустите приложение:
```
dotnet run
```

Приложение будет доступно по адресу: http://localhost:8080.

### Запуск в Docker контейнере
Запустите приложение с помощью Docker Compose:
```bash
docker compose up -d --build
```

База данных будет запущена в контейнере.

Приложение будет доступно по адресу: http://localhost:8080.

# Функциональные возможности

- Регистрация и аутентификация пользователей
- Создание и управление лотами
- Загрузка фотографий
- Просмотр лотов
- Размещение ставок
- Выкуп выигранных лотов

Баймурадов Тимур
