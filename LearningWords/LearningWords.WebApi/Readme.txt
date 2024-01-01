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
dotnet ef migrations add -p LearningWords.Migrations --startup-project LearningWords.WebApi -o Migrations -c WordsDbContext InitialCreate
dotnet ef migrations add -p LearningWords.Collections.Identity.Migrations --startup-project LearningWords.Collections.Identity -o Migrations -c CollectionIdentityDbContext InitialCreate

Обновление миграций:
dotnet ef database update -p IdentityService.WebApi --startup-project IdentityService.WebApi -c IdentityContext
dotnet ef database update -p LearningWords.WebApi --startup-project LearningWords.WebApi -c WordsDbContext
dotnet ef database update -p LearningWords.Collections.Identity --startup-project LearningWords.Collections.Identity -c CollectionIdentityDbContext

hofd@mail.ru
WarhackHD!1

TODO:
1. Одна карточка на несколько коллекций?
2. А точно надо поле DeletedAt, зачем?
3. Скрыть WordService?
4. Регистрация с подтверждением почты.
5. Перезапрос токена.