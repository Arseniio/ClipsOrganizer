# ClipsOrganizer

**ClipsOrganizer** — это приложение для управления коллекциями видеоматериалов, предоставляющее функции обработки и экспорта видео. Основное назначение программы — организация видеоконтента в удобную структуру и выполнение базовых операций, таких как обрезка и перекодирование файлов.

## 📋 Основные возможности

- Организация видеоматериалов в коллекции.
- Редактирование видео с использованием обрезки.
- Перекодирование видео в различные форматы с использованием `ffmpeg`.
- Экспорт обработанных файлов в пользовательские директории.

## ⚙️ Технологии

- **Язык программирования:** C#
- **Библиотеки:** WPF для пользовательского интерфейса.
- **Инструмент обработки видео:** ffmpeg.

## 🚀 Установка и запуск

### Требования

- Windows 10 или новее.
- .NET Framework 4.7.2 или новее.
- ffmpeg (должен быть установлен локально).

### Шаги установки

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/ваш-пользователь/ClipsOrganizer.git
   cd ClipsOrganizer
   ```
    Убедитесь, что ffmpeg.exe находится в одной из папок вашего проекта или укажите его путь в settings.json программы.
    Соберите проект через Visual Studio:
        Откройте файл решения .sln.
        Постройте проект (Build → Build Solution).
    Запустите приложение.
    
## 🛠️ Конфигурация

Для корректной работы приложения необходимо указать при первом запуске:
- Путь к директории, где будут храниться коллекции видеоматериалов.
- Путь к ffmpeg.exe в настройках программы.

## Использование горячих клавиш

Приложение поддерживает следующие горячие клавиши для удобного управления процессом обрезки и перекодирования видео:

| Горячая клавиша | Действие                                                                                 |
|------------------|-----------------------------------------------------------------------------------------|
| **C**            | Устанавливает **начало обрезки** в текущей позиции.                                     |
| **E**            | Устанавливает **конец обрезки** в текущей позиции. Может использоваться для обрезки с начала файла до указанной точки. |
| **D**            | Выполняет **перекодирование всего файла** без обрезки.                                  |
| **Shift + C**    | Устанавливает **обрезку до конца файла** начиная с текущей позиции.                     |



## Статус проекта
На данный момент проект всё ещё находится в разработке, по этому билд не создан намеренно, после уверенности в работоспособности приложения будет выложен первый билд.

English (Auto Translated)
------------------
# ClipsOrganizer

**ClipsOrganizer** is a video collection management application that provides video processing and export functions. The main purpose of the program is to organize video content into a convenient structure and perform basic operations, such as trimming and transcoding files.

## 📋 Main features

- Organizing video content into collections.
- Editing videos using trimming.
- Video transcoding into different formats using `ffmpeg`.
- Export processed files to user directories.

## ⚙️ Technologies

- **Programming Language:** C#
- **Libraries:** WPF for user interface.
- **Video processing tool:** ffmpeg.

## 🚀 Installation and startup

### Requirements

- Windows 10 or newer.
- .NET Framework 4.7.2 or newer.
- ffmpeg (must be installed locally).

### Installation Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/ваш-пользователь/ClipsOrganizer.git
   cd ClipsOrganizer
   ```
    Make sure that ffmpeg.exe is in one of your project folders or specify its path in settings.json of the program.
    Build the project through Visual Studio:
        Open the .sln solution file.
        Build the project (Build → Build Solution).
    Run the application.
    
## 🛠️ Configuration

For the application to work correctly, you must specify at the first run:
- The path to the directory where the video collections will be stored.
- Path to ffmpeg.exe in the program settings.

## Using hotkeys

The application supports the following hotkeys to easily control the video trimming and transcoding process:

| Hotkey | Action |
|------------------|-----------------------------------------------------------------------------------------|
| **C** | Sets **start trimming** at the current position. |
| **E** | Sets the **end of trimming** at the current position. Can be used to trim from the beginning of the file to a specified point. |
| **D** | **Encodes the entire file** without trimming. |
| **Shift + C** | Sets **trim to end of file** starting at the current position. | |



## Project Status
At the moment the project is still under development, so the build is not created intentionally, after we are sure that the application works, the first build will be posted.

