using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

class Program
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    static extern void mouse_event(
        uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    const uint LEFTDOWN = 0x0002;
    const uint LEFTUP = 0x0004;

    static readonly string ProjectFolder =
        Directory.GetCurrentDirectory();

    static readonly string GameFolder =
        @"C:\Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool";

    static readonly string ResultsFolder =
        Path.Combine(ProjectFolder, "results");

    static readonly string DesktopScreenshot =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "wukong-auto-result.png");

    record FpsResult(
        string Average,
        string Maximum,
        string Minimum,
        string Percentile);

        static bool ActivateBenchmark()
        {
            foreach (Process process in Process.GetProcessesByName("b1-Win64-Shipping"))
            {
                try
                {
                    process.Refresh();

        Console.WriteLine(
            $"Найден процесс: {process.ProcessName}, " +
            $"PID: {process.Id}, " +
            $"Окно: {process.MainWindowHandle}, " +
            $"Заголовок: {process.MainWindowTitle}");

                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, 9);
                        SetForegroundWindow(process.MainWindowHandle);

                        Thread.Sleep(1500);

                        if (IsBenchmarkActive())
                            return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка активации окна: " + ex.Message);
                }
                finally
                {
                    process.Dispose();
                }
            }

            return false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static bool IsBenchmarkActive()
        {
            IntPtr activeWindow = GetForegroundWindow();

            if (activeWindow == IntPtr.Zero)
                return false;

            GetWindowThreadProcessId(activeWindow, out uint processId);

            try
            {
                using var process = Process.GetProcessById((int)processId);

                return process.ProcessName.Equals("b1-Win64-Shipping",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool GetCursorPos(out Point point);

    static void Click(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(500);

            mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(250);
            mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);

            Thread.Sleep(500);
        }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
static extern bool GetClientRect(
    IntPtr hWnd,
    out Rectangle rect);

[System.Runtime.InteropServices.DllImport("user32.dll")]
static extern bool ClientToScreen(
    IntPtr hWnd,
    ref Point point);

static void ClickGame(int x, int y)
{
    foreach (Process process in
        Process.GetProcessesByName("b1-Win64-Shipping"))
    {
        try
        {
            process.Refresh();

            IntPtr window = process.MainWindowHandle;

            if (window == IntPtr.Zero)
                continue;

            if (!GetClientRect(window, out Rectangle rect))
                continue;

            Point origin = new Point(0, 0);

            if (!ClientToScreen(window, ref origin))
                continue;

            int screenX = origin.X +
                (int)(x * rect.Width / 1920.0);

            int screenY = origin.Y +
                (int)(y * rect.Height / 1080.0);

            Console.WriteLine(
                $"Клик в окне игры: {screenX}, {screenY}");

            Click(screenX, screenY);
            return;
        }
        finally
        {
            process.Dispose();
        }
    }

    throw new Exception("Не удалось найти окно игры для клика.");
}

    static async Task Main(string[] args)
    {
        SetProcessDPIAware();
        
        Console.OutputEncoding = Encoding.UTF8;

        Directory.CreateDirectory(ResultsFolder);

        if (args.Contains("--check-window"))
        {
            bool activated = ActivateBenchmark();
            Console.WriteLine(activated
                ? "УСПЕХ: окно игры найдено и активно!"
                : "ОШИБКА: окно игры не удалось активировать.");
            return;
        }

        if (args.Contains("--report"))
        {
            string lowScreenshot = Path.Combine(ResultsFolder, "cpu-low.png");
            string highScreenshot = Path.Combine(ResultsFolder, "gpu-high.png");

            FpsResult low = await ReadResults(lowScreenshot);
            FpsResult high = await ReadResults(highScreenshot);

            if (!IsComplete(low) || !IsComplete(high))
            {
                Console.WriteLine("Ошибка: не все показатели FPS распознаны.");
                return;
            }

            SaveReport(low, high);
            return;
        }

        if (args.Contains("--read"))
        {
            string screenshot = args.Contains("--low")
                ? Path.Combine(ResultsFolder, "cpu-low.png")
                : args.Contains("--high")
                    ? Path.Combine(ResultsFolder, "gpu-high.png")
                    : DesktopScreenshot;

            await ReadResults(screenshot);
            return;
        }

        if (args.Contains("--all"))
        {
            await RunAllTests();
            return;
        }

        string preset = args.Contains("--high") ? "high" : "low";

        await RunTest(preset, DesktopScreenshot);
    }

    static async Task RunAllTests()
    {
        Console.WriteLine("=== АВТОМАТИЧЕСКИЙ ЗАПУСК ДВУХ ТЕСТОВ ===");

        string lowScreenshot =
            Path.Combine(ResultsFolder, "cpu-low.png");

        string highScreenshot =
            Path.Combine(ResultsFolder, "gpu-high.png");

        bool lowCompleted = await RunTest("low", lowScreenshot);

        if (!lowCompleted)
        {
            Console.WriteLine("LOW-тест не завершён. Остановка.");
            return;
        }

        CloseBenchmark();
        Console.WriteLine(
            "Осталось процессов игры: " +
            Process.GetProcessesByName("b1_benchmark").Length);
        Console.WriteLine("Пауза перед вторым тестом...");
        await Task.Delay(5000);

        bool highCompleted = await RunTest("high", highScreenshot);

        if (!highCompleted)
        {
            Console.WriteLine("HIGH-тест не завершён. Остановка.");
            return;
        }

        CloseBenchmark();

        Console.WriteLine();
        Console.WriteLine("=== ФОРМИРОВАНИЕ ОТЧЁТА ===");

        FpsResult low = await ReadResults(lowScreenshot);
        FpsResult high = await ReadResults(highScreenshot);

        SaveReport(low, high);

        Console.WriteLine();
        Console.WriteLine("Оба теста завершены!");
        Console.WriteLine("Результаты находятся в папке results.");
    }

    static async Task<bool> RunTest(
        string preset,
        string screenshotPath)
    {
        string gameExe =
            Path.Combine(GameFolder, "b1_benchmark.exe");

        string presetPath = Path.Combine(
            ProjectFolder,
            $"GameUserSettings.{preset}.ini");

        string gameSettingsPath = Path.Combine(
            GameFolder,
            "b1", "Saved", "Config", "Windows",
            "GameUserSettings.ini");

        if (!File.Exists(gameExe))
        {
            Console.WriteLine("Не найден файл игры: " + gameExe);
            return false;
        }

        if (!File.Exists(presetPath))
        {
            Console.WriteLine("Не найден пресет: " + presetPath);
            return false;
        }

        if (Process.GetProcessesByName("b1_benchmark").Length > 0)
        {
            Console.WriteLine("Сначала необходимо закрыть игру.");
            return false;
        }

        File.Copy(presetPath, gameSettingsPath, true);

        Console.WriteLine();
        Console.WriteLine($"=== ЗАПУСК {preset.ToUpper()} ===");
        Console.WriteLine("Настройки установлены.");

        Process.Start(new ProcessStartInfo
        {
            FileName = "steam://rungameid/3132990",
            UseShellExecute = true
        });

        Console.WriteLine("Ожидаем запуска игры...");

        bool gameStarted = false;

        for (int i = 0; i < 60; i++)
        {
            if (Process.GetProcessesByName("b1_benchmark").Length > 0)
            {
                gameStarted = true;
                break;
            }

            await Task.Delay(1000);
        }

        if (!gameStarted)
        {
            Console.WriteLine("Ошибка: игра не запустилась за 60 секунд.");
            return false;
        }

        Console.WriteLine("Процесс игры найден. Ожидаем загрузку окна...");
        await Task.Delay(30000);

        if (!ActivateBenchmark())
        {
            Console.WriteLine("Ошибка: окно бенчмарка не активно. Клики отменены.");
            return false;
        }

        Console.WriteLine("Пропускаем заставку...");
        ClickGame(960, 540);
        await Task.Delay(8000);

        Console.WriteLine("Нажимаем «Тест быстродействия»...");
        ClickGame(190, 470);
        await Task.Delay(2500);

        Console.WriteLine("Нажимаем «Подтвердить»...");
        ClickGame(600, 650);

        if (Environment.GetCommandLineArgs().Contains("--test-clicks"))
        {
            Console.WriteLine();
            Console.WriteLine("=== ПРОВЕРКА КЛИКОВ ЗАВЕРШЕНА ===");
            Console.WriteLine("Посмотри, запустился ли тест быстродействия.");
            Console.WriteLine("Ожидание 4 минут отключено.");
        return false;
        }

        Console.WriteLine("Ожидаем завершения теста (4 минуты)...");
        await Task.Delay(240000);

        var screen = Screen.PrimaryScreen!.Bounds;

        using (var bitmap = new Bitmap(
            screen.Width, screen.Height))
        {
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                screen.Left,
                screen.Top,
                0,
                0,
                screen.Size);

            bitmap.Save(screenshotPath, ImageFormat.Png);
        }

        Console.WriteLine("Скриншот сохранён: " + screenshotPath);

        FpsResult result = await ReadResults(screenshotPath);

        if (!IsComplete(result))
        {
            Console.WriteLine(
                "Не все показатели FPS распознаны. " +
                "Проверь скриншот перед продолжением.");

            return false;
        }

        return true;
    }

    static bool IsComplete(FpsResult result)
    {
        return !string.IsNullOrEmpty(result.Average)
            && !string.IsNullOrEmpty(result.Maximum)
            && !string.IsNullOrEmpty(result.Minimum)
            && !string.IsNullOrEmpty(result.Percentile);
    }

    static void CloseBenchmark()
        {
            Console.WriteLine("Закрываем бенчмарк...");

            string[] processNames =
            {
                "b1-Win64-Shipping",
                "b1_benchmark"
            };

            foreach (string processName in processNames)
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        Console.WriteLine("Закрываем процесс: " + process.ProcessName);

                        if (!process.HasExited)
                        {
                            if (!process.CloseMainWindow() ||
                                !process.WaitForExit(10000))
                            {
                                process.Kill();
                            }

                            process.WaitForExit(10000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Не удалось закрыть процесс: " + ex.Message);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }

            Console.WriteLine("Закрытие бенчмарка завершено.");
        }

    static async Task<string> RecognizeImage(string imagePath)
    {
        StorageFile file =
            await StorageFile.GetFileFromPathAsync(imagePath);

        using var stream = await file.OpenReadAsync();

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync();

        var engine =
            OcrEngine.TryCreateFromLanguage(new Language("ru-RU"));

        if (engine == null)
            throw new Exception("Не удалось запустить Windows OCR.");

        var result = await engine.RecognizeAsync(bitmap);

        return result.Text;
    }

    static async Task<FpsResult> ReadResults(string screenshotPath)
    {
        if (!File.Exists(screenshotPath))
        {
            Console.WriteLine("Скриншот не найден: " + screenshotPath);
            return new FpsResult("", "", "", "");
        }

        string text = await RecognizeImage(screenshotPath);

        Console.WriteLine();
        Console.WriteLine("=== РАСПОЗНАННЫЙ ТЕКСТ ===");
        Console.WriteLine(text);

        string maximum = Regex.Match(
            text,
            @"Максимум\s+(\d{1,3})(?=\s|FPS|$)",
            RegexOptions.IgnoreCase
        ).Groups[1].Value;

        string minimum = Regex.Match(
        text,
            @"Минимум\s+(\d{1,3})(?=\s|FPS|$)",
            RegexOptions.IgnoreCase
        ).Groups[1].Value;

        string percentile = Regex.Match(
            text,
            @"5\s*[-–]?\s*й\s+перцентиль\s+(\d{1,3})(?=\s|FPS|$)",
            RegexOptions.IgnoreCase
        ).Groups[1].Value;

        string average = Regex.Match(
            text,
            @"В\s+среднем\s+(\d{1,3})(?=\s|FPS|$)",
            RegexOptions.IgnoreCase
        ).Groups[1].Value;

        if (string.IsNullOrEmpty(average))
        {
            Console.WriteLine(
                "Увеличиваем область со средним FPS...");

            string croppedPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop),
                "wukong-fps-crop.png");

            using (var original = new Bitmap(screenshotPath))
            {
                int x = (int)(65.0 / 1649 * original.Width);
                int y = (int)(300.0 / 928 * original.Height);
                bool isLow = screenshotPath.Contains("cpu-low", StringComparison.OrdinalIgnoreCase);

                int w = (int)(150.0 / 1649 * original.Width);
                int h = (int)(60.0 / 928 * original.Height);

                var region = new Rectangle(x, y, w, h);

                using var crop = original.Clone(
                    region,
                    PixelFormat.Format32bppArgb);

                using var enlarged = new Bitmap(
                    crop.Width * 2,
                    crop.Height * 2);

                using var graphics = Graphics.FromImage(enlarged);

                graphics.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                graphics.DrawImage(
                    crop,
                    0,
                    0,
                    enlarged.Width,
                    enlarged.Height);

                enlarged.Save(croppedPath, ImageFormat.Png);
            }

            string croppedText =
                await RecognizeImage(croppedPath);

            Console.WriteLine(
                "Текст увеличенного фрагмента: " + croppedText);

            average = Regex.Match(
                croppedText,
                @"(\d+)\s*FPS",
                RegexOptions.IgnoreCase
            ).Groups[1].Value;

            if (string.IsNullOrEmpty(average))
{
    average = Regex.Match(croppedText, @"\d{2,3}").Value;
}

        if (string.IsNullOrEmpty(average))
        {
            Console.WriteLine("Пробуем распознать контрастное изображение...");

            string contrastPath = Path.Combine(
                Path.GetDirectoryName(croppedPath)!,
                "wukong-fps-contrast.png");

            using (var image = new Bitmap(croppedPath))
            using (var contrast = new Bitmap(image.Width, image.Height))
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Color pixel = image.GetPixel(x, y);

                        int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                        Color newColor = brightness >= 85
                            ? Color.White
                            : Color.Black;

                        contrast.SetPixel(x, y, newColor);
                    }
                }

                contrast.Save(contrastPath, ImageFormat.Png);
            }

            string contrastText = await RecognizeImage(contrastPath);

            Console.WriteLine("Контрастный OCR: " + contrastText);

            average = Regex.Match(
                contrastText,
                @"\d{2,3}"
            ).Value;
        }
        }

        Console.WriteLine();
        Console.WriteLine("=== РЕЗУЛЬТАТЫ ТЕСТА ===");
        Console.WriteLine("Средний FPS: " + average);
        Console.WriteLine("Максимальный FPS: " + maximum);
        Console.WriteLine("Минимальный FPS: " + minimum);
        Console.WriteLine("5-й перцентиль: " + percentile);

        return new FpsResult(
            average,
            maximum,
            minimum,
            percentile);
    }

    static void SaveReport(FpsResult low, FpsResult high)
    {
        var report = new StringBuilder();

        report.AppendLine(
            "# Black Myth: Wukong Benchmark — отчёт");

        report.AppendLine();
        report.AppendLine(
            $"Дата создания: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        report.AppendLine();
        report.AppendLine("## Характеристики компьютера");
        report.AppendLine();
        report.AppendLine("- ОС: Windows 10, 64-bit");
        report.AppendLine("- CPU: Intel Core i5-12500H");
        report.AppendLine(
            "- GPU: NVIDIA GeForce RTX 3060 Laptop GPU");
        report.AppendLine("- Видеопамять: 6 GB");
        report.AppendLine("- RAM: 16 GB");
        report.AppendLine("- Разрешение: 1920 × 1080");

        report.AppendLine();
        report.AppendLine("## Результаты FPS");
        report.AppendLine();
        report.AppendLine("| Показатель | LOW | HIGH |");
        report.AppendLine("|---|---:|---:|");

        report.AppendLine(
            $"| Средний FPS | {low.Average} | {high.Average} |");

        report.AppendLine(
            $"| Максимальный FPS | {low.Maximum} | {high.Maximum} |");

        report.AppendLine(
            $"| Минимальный FPS | {low.Minimum} | {high.Minimum} |");

        report.AppendLine(
            $"| 5-й перцентиль FPS | {low.Percentile} | {high.Percentile} |");

        report.AppendLine();
        report.AppendLine("## Настройки графики");
        report.AppendLine();

        foreach (string preset in new[] { "low", "high" })
        {
            string path = Path.Combine(
                ProjectFolder,
                $"GameUserSettings.{preset}.ini");

            report.AppendLine($"### {preset.ToUpper()}");
            report.AppendLine();

            if (File.Exists(path))
            {
                report.AppendLine("```ini");
                report.AppendLine(File.ReadAllText(path).TrimEnd());
                report.AppendLine("```");
            }
            else
            {
                report.AppendLine("Файл настроек не найден.");
            }

            report.AppendLine();
        }

        report.AppendLine("## Скриншоты");
        report.AppendLine();
        report.AppendLine("### LOW");
        report.AppendLine("![LOW](cpu-low.png)");
        report.AppendLine();
        report.AppendLine("### HIGH");
        report.AppendLine("![HIGH](gpu-high.png)");
        report.AppendLine();
        report.AppendLine(
            "LOW и HIGH обозначают прогоны с низкими " +
            "и высокими настройками графики. " +
            "Тип ограничения производительности " +
            "(CPU/GPU) отдельно не измерялся.");

        string reportPath =
            Path.Combine(ResultsFolder, "report.md");

        File.WriteAllText(
            reportPath,
            report.ToString(),
            new UTF8Encoding(false));

        Console.WriteLine("Отчёт создан: " + reportPath);
    }
}
