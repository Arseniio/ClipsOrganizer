# ClipsOrganizer

**ClipsOrganizer** — это приложение для управления коллекциями видеоматериалов и изображений, предоставляющее функции обработки и экспорта медиафайлов. Основное назначение программы — организация медиа-контента в удобную структуру и выполнение базовых операций, таких как обрезка, перекодирование и обработка файлов.

## 📋 Основные возможности

### Видео
- Организация видеоматериалов в коллекции
- Редактирование видео с использованием обрезки
- Перекодирование видео в различные форматы с использованием `ffmpeg`
- Экспорт обработанных файлов в пользовательские директории
- Управление воспроизведением и навигация по видео
- Настройка громкости и других параметров воспроизведения
- Поддержка аппаратного ускорения (NVENC, QSV, AMF)
- Двухпроходное кодирование для улучшения качества
- Настройка параметров аудио (кодек, битрейт, каналы)

### Изображения
- Просмотр и организация изображений в коллекции
- Поддержка различных форматов изображений (JPEG, PNG, BMP, GIF, TIFF, WEBP, HEIF, AVIF, RAW и др.)
- Обработка и экспорт изображений с настройкой:
  - Разрешения
  - Качества сжатия
  - Цветового профиля (sRGB, Adobe RGB)
  - Сохранения метаданных
- Просмотр метаданных изображений (EXIF, IPTC)
- Масштабирование и панорамирование изображений
- Поддержка RAW-файлов различных производителей (CR2, NEF, ARW, ORF, RW2)

## ⚙️ Технологии

- **Язык программирования:** C#
- **Библиотеки:** WPF для пользовательского интерфейса
- **Инструменты обработки:**
  - ffmpeg для видео
  - ImageMagick для изображений
  - MetadataExtractor для работы с метаданными

## 🚀 Установка и запуск

### Требования

- Windows 10 или новее
- .NET Framework 4.7.2 или новее
- ffmpeg (должен быть установлен локально)

### Шаги установки

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/ваш-пользователь/ClipsOrganizer.git
   cd ClipsOrganizer
   ```
2. Убедитесь, что ffmpeg.exe находится в одной из папок вашего проекта или укажите его путь в settings.json программы
3. Соберите проект через Visual Studio:
   - Откройте файл решения .sln
   - Постройте проект (Build → Build Solution)
4. Запустите приложение

## 🛠️ Конфигурация

### Настройки приложения
Для корректной работы приложения необходимо указать при первом запуске:
- Путь к директории, где будут храниться коллекции медиафайлов
- Путь к ffmpeg.exe в настройках программы

### Настройки экспорта видео
- **Видеокодек:** Выбор кодека для кодирования (H.264, H.265, VP8, VP9, AV1 и др.)
- **Битрейт видео:** Настройка качества видео (kbps)
- **CRF:** Настройка качества для кодирования с постоянным качеством (0-51)
- **Разрешение:** Выбор разрешения выходного видео (Оригинальное, 720p, 1080p, 4K, Пользовательское)
- **Частота кадров:** Настройка частоты кадров выходного видео
- **Двухпроходное кодирование:** Улучшение качества кодирования
- **Аудиокодек:** Выбор кодека для аудио (AAC, MP3, Opus и др.)
- **Битрейт аудио:** Настройка качества аудио (kbps)
- **Нормализация звука:** Выравнивание громкости аудио
- **Аппаратное ускорение:** Использование GPU для кодирования (NVENC, QSV, AMF)

### Настройки экспорта изображений
- **Формат:** Выбор формата выходного изображения (JPEG, PNG, WEBP, HEIF, AVIF и др.)
- **Качество:** Настройка уровня сжатия (1-100)
- **Разрешение:** Настройка размеров выходного изображения
- **Цветовой профиль:** Выбор цветового пространства (sRGB, Adobe RGB, ProPhoto RGB)
- **Сохранение метаданных:** Сохранение EXIF и других метаданных

### Дополнительные настройки
- **Автоплей:** Автоматическое воспроизведение видео при открытии
- **Смещение автоплея:** Начальная позиция для автоплея
- **Открытие папки после экспорта:** Автоматическое открытие папки с результатами
- **Параллельный экспорт:** Одновременная обработка нескольких файлов
- **Использование всех потоков:** Максимальное использование процессора для кодирования

## Использование горячих клавиш

### Управление видео
| Горячая клавиша | Действие |
|------------------|-----------------------------------------------------------------------------------------|
| **Стрелка влево** | Перемотка назад на 1 секунду |
| **Стрелка вправо** | Перемотка вперед на 1 секунду |
| **Ctrl + Стрелка вверх** | Увеличение громкости |
| **Ctrl + Стрелка вниз** | Уменьшение громкости |
| **Пробел** | Переключение воспроизведения (пауза/проигрывание) |
| **C** | Установка точки начала обрезки |
| **E** | Установка точки завершения обрезки |
| **Shift + C** | Обрезка от текущего момента до конца видео |
| **D** | Открытие окна перекодирования для всего файла |
| **S** | Очистка выделения |

### Общие горячие клавиши
| Горячая клавиша | Действие |
|------------------|-----------------------------------------------------------------------------------------|
| **Enter** | Открыть выбранный файл |
| **M** | Добавить выбранный файл в коллекцию |
| **Ctrl + S** | Сохранить настройки |

## Статус проекта

На данный момент проект всё ещё находится в разработке, по этому билд не создан намеренно, после уверенности в работоспособности приложения будет выложен первый билд.

English (Auto Translated)
------------------
# ClipsOrganizer

**ClipsOrganizer** is a video and image collection management application that provides media file processing and export functions. The main purpose of the program is to organize media content into a convenient structure and perform basic operations such as trimming, transcoding and file processing.

## 📋 Main features

### Video
- Organizing video content into collections
- Editing videos using trimming
- Video transcoding into different formats using `ffmpeg`
- Exporting processed files to user directories
- Playback control and video navigation
- Volume and other playback settings adjustment
- Hardware acceleration support (NVENC, QSV, AMF)
- Two-pass encoding for improved quality
- Audio settings configuration (codec, bitrate, channels)

### Images
- Viewing and organizing images in collections
- Support for various image formats (JPEG, PNG, BMP, GIF, TIFF, WEBP, HEIF, AVIF, RAW, etc.)
- Image processing and export with settings for:
  - Resolution
  - Compression quality
  - Color profile (sRGB, Adobe RGB)
  - Metadata preservation
- Viewing image metadata (EXIF, IPTC)
- Image scaling and panning
- Support for RAW files from various manufacturers (CR2, NEF, ARW, ORF, RW2)

## ⚙️ Technologies

- **Programming Language:** C#
- **Libraries:** WPF for user interface
- **Processing Tools:**
  - ffmpeg for video
  - ImageMagick for images
  - MetadataExtractor for metadata handling

## 🚀 Installation and startup

### Requirements

- Windows 10 or newer
- .NET Framework 4.7.2 or newer
- ffmpeg (must be installed locally)

### Installation Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/your-user/ClipsOrganizer.git
   cd ClipsOrganizer
   ```
2. Make sure that ffmpeg.exe is in one of your project folders or specify its path in settings.json of the program
3. Build the project through Visual Studio:
   - Open the .sln solution file
   - Build the project (Build → Build Solution)
4. Run the application

## 🛠️ Configuration

### Application Settings
For the application to work correctly, you must specify at the first run:
- The path to the directory where media collections will be stored
- Path to ffmpeg.exe in the program settings

### Video Export Settings
- **Video Codec:** Codec selection for encoding (H.264, H.265, VP8, VP9, AV1, etc.)
- **Video Bitrate:** Video quality setting (kbps)
- **CRF:** Quality setting for constant quality encoding (0-51)
- **Resolution:** Output video resolution selection (Original, 720p, 1080p, 4K, Custom)
- **Frame Rate:** Output video frame rate setting
- **Two-pass Encoding:** Improved encoding quality
- **Audio Codec:** Audio codec selection (AAC, MP3, Opus, etc.)
- **Audio Bitrate:** Audio quality setting (kbps)
- **Audio Normalization:** Audio volume leveling
- **Hardware Acceleration:** GPU usage for encoding (NVENC, QSV, AMF)

### Image Export Settings
- **Format:** Output image format selection (JPEG, PNG, WEBP, HEIF, AVIF, etc.)
- **Quality:** Compression level setting (1-100)
- **Resolution:** Output image dimensions setting
- **Color Profile:** Color space selection (sRGB, Adobe RGB, ProPhoto RGB)
- **Metadata Preservation:** EXIF and other metadata preservation

### Additional Settings
- **AutoPlay:** Automatic video playback on open
- **AutoPlay Offset:** Initial position for autoplay
- **Open Folder After Export:** Automatic opening of results folder
- **Parallel Export:** Simultaneous processing of multiple files
- **Use All Threads:** Maximum CPU usage for encoding

## Using hotkeys

### Video Control
| Hotkey | Action |
|------------------|-----------------------------------------------------------------------------------------|
| **Left Arrow** | Rewind 1 second |
| **Right Arrow** | Forward 1 second |
| **Ctrl + Up Arrow** | Increase volume |
| **Ctrl + Down Arrow** | Decrease volume |
| **Space** | Toggle playback (pause/play) |
| **C** | Set trim start point |
| **E** | Set trim end point |
| **Shift + C** | Trim from current position to end of video |
| **D** | Open encoding window for entire file |
| **S** | Clear selection |

### General Hotkeys
| Hotkey | Action |
|------------------|-----------------------------------------------------------------------------------------|
| **Enter** | Open selected file |
| **M** | Add selected file to collection |
| **Ctrl + S** | Save settings |

## Project Status
At the moment the project is still under development, so the build is not created intentionally, after we are sure that the application works, the first build will be posted.

