Какие функции нужны:
1. Создать коллекцию. +
2. Переименовать коллекцию. +
3. Удалить коллекцию. +
4. Просмотреть все карточки в коллекции. Посмотреть все мои коллекции. +
5. Режим учебы (выводить карточки по заданному алгоритму).
6. Создать карточку и добавить в нее слова. +
7. Добавить карточку в коллекцию.
8. Удалить карточку из коллекции.
9. Посмотреть, в каких коллекциях карточка есть.
10. Удалить карточку совсем. +
11. Редактировать карточку и слова в ней. При этом сбрасывать статистику карточки. +
12. Получить все карточки в коллекции. +

Создание миграций:
dotnet ef migrations add -p IdentityService.Migrations --startup-project IdentityService.WebApi -o Migrations -c IdentityContext InitialCreate
dotnet ef migrations add -p LearnWord.Migrations --startup-project LearnWord.WebApi -o Migrations -c WordsDbContext InitialCreate
dotnet ef migrations add -p LearnWord.Collections.Identity.Migrations --startup-project LearnWord.Collections.Identity -o Migrations -c CollectionIdentityDbContext InitialCreate

Обновление миграций:
dotnet ef database update -p IdentityService.WebApi --startup-project IdentityService.WebApi -c IdentityContext
dotnet ef database update -p LearnWord.WebApi --startup-project LearnWord.WebApi -c WordsDbContext
dotnet ef database update -p LearnWord.Collections.Identity --startup-project LearnWord.Collections.Identity -c CollectionIdentityDbContext

hofd@mail.ru
WarhackHD!1

LearnWord.Gateway - 44335
IdentityService - 44379
LearnWord.CollectionsIdentity - 44345
LearnWord.WebApi - 44350

TODO:
1. Одна карточка на несколько коллекций?
2. А точно надо поле DeletedAt, зачем?
3. Скрыть WordService?
4. Регистрация с подтверждением почты.
5. Перезапрос токена.