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
dotnet ef migrations add -p LearningWords.WebApp --startup-project LearningWords.WebApp -o Data\Migrations -c ApplicationDbContext InitialCreate
dotnet ef migrations add -p LearningWords.Migrations --startup-project LearningWords.WebApi -o Migrations -c WordsDbContext InitialCreate

Обновление миграций:
dotnet ef database update -p LearningWords.WebApp --startup-project LearningWords.WebApp -c ApplicationDbContext
dotnet ef database update -p LearningWords.WebApi --startup-project LearningWords.WebApi -c WordsDbContext

hofd@mail.ru
WarhackHD!1

TODO:
1. Одна карточка на несколько коллекций?
2. А точно надо поле DeletedAt, зачем?