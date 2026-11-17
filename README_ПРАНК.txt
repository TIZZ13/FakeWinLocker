WinLockerKFC (Fake WinLocker) — шуточный «винлокер» для пранка
================================================================

RU:
===

ЧТО ЭТО
Полноэкранная шутка в стиле старого «Windows заблокирован»:
- синий экран как на референсе;
- таймер 8:00;
- пароль 3015;
- музыка: ICEGERGERT — «Агент провокатор», старт с 9-й секунды;
- случайные звуки (коты, собаки, пуки, кукареканье и т.д.) каждые 5–10 секунд,
  иногда по 1, иногда по 2–3 одновременно;
- коты всегда летают по экрану с радужным следом из-под хвоста и иногда
  ускоряются/замедляются;
- на кукареканье по низу экрана выбегают курицы;
- вёдра КФС полностью непрозрачные, летают, вращаются и отскакивают
  друг от друга, постепенно их становится больше (до 30 штук);
  коты и курицы на физику вёдер не влияют;
- на неверный код — звук стекла, тряска экрана и «трещины»;
- последние 30 секунд экран пульсирует красным, слышно сердцебиение;
- секретный disco-режим по клавишам K, F, C (радужный фон, громкая музыка);
- достижения в стиле уведомлений Windows 7 (с фирменным звуком)
  и фейковая капча с британскими вислоухими котами в стиле диалога Aero;
- клик по коту — только мяу, клик по ведру — из него вылетают жареные
  крылышки; курицы — просто декорация;
- на финале — фанфары и кукареканье;
- иконка exe — фиолетовая стеклянная «S» со звёздами (Skalioz Free).

Проект создан для безвредного пранка над другом, который в курсе съёмки.
Приложение ничего не шифрует, не удаляет, не ворует и не прописывается
в автозагрузку. Единственный «жёсткий» эффект — принудительная перезагрузка
по истечении таймера, поэтому несохранённые документы будут закрыты.

ВАЖНО ПРО ФАЙЛ
Отдавать/запускать нужно только WinLockerKFC.exe (один файл, ~9,8 МБ).
Имя файла можно менять (например, на Skalioz Free Launcher.exe) — на работу
это не влияет.
Внутри него лежит упакованная рабочая часть. При запуске WinLockerKFC.exe:
1. распаковывает себя в %LOCALAPPDATA%\Microsoft\Windows\WUDFHost.exe;
2. запускает этот файл и сразу закрывается сам.

То есть в системе работает процесс с именем WUDFHost.exe и «майкрософтовской»
подписью в свойствах файла. Рядом с настоящим системным WUDFHost.exe
(он живёт в сеансе Services) в Диспетчере задач появляется второй WUDFHost.exe
в сеансе Console — под нашим пользователем. Это косметическая маскировка,
а не настоящее сокрытие: путь процесса всё равно виден.
В свойствах самого WinLockerKFC.exe: «Описание» — Skalioz Free Loader,
«Организация» — Skalioz Best.

КОНТРОЛЬНЫЕ СУММЫ ЭТОЙ СБОРКИ (SHA-256)
Файл               Размер      SHA-256
WinLockerKFC.exe   9 822 720   7C27BAECA33FE6C5DEEA7CC9C73795443B352902F279E86A1A29BB58A9798F86
WinLockerKFC.7z    9 938 603   CD2337B4718F29F129F29C942C8C7823635B2E70C39411D595CEA0507FDDD169
Суммы меняются при каждой пересборке; сверять через
Get-FileHash <файл> -Algorithm SHA256 в PowerShell.
Архив WinLockerKFC.7z запаролен: пароль 123 (шифруются и содержимое,
и имена файлов). Внутри лежит WinLockerKFC.exe.

ЧЕСТНО ПРО МАСКИРОВКУ
Это косметическая маскировка, а не настоящее сокрытие:
- имя файла, описание, версия, иконка и заголовок окна совпадают с системным
  WUDFHost;
- но в Диспетчере задач, если включить колонку «Путь к образу»/«Командная
  строка» или открыть «Свойства → Расположение», видно
  C:\Users\<юзер>\AppData\Local\Microsoft\Windows\WUDFHost.exe;
- настоящее сокрытие процесса из пользовательского режима невозможно.
Друг разрешил маскировку, поэтому это ок.

ЧТО ПРОИСХОДИТ
1. Запускаешь WinLockerKFC.exe.
   Для полного ограничения Ctrl+Alt+Del — правой кнопкой →
   «Запуск от имени администратора» (см. раздел ниже).
2. Экран блокируется. Alt+Tab, Win, Ctrl+Esc, Ctrl+Shift+Esc, Alt+F4 блокируются.
   Диспетчер задач закрывается, если его открыть.
3. Вводишь код 3015 кнопками на экране или с клавиатуры, нажимаешь
   «ВХОД В СИСТЕМУ» / Enter.
   Дальше появляется фейковая капча «Выберите все квадратики с британскими
   вислоухими» (правильные — коты, ведёрки и куры — обманки).
   Она специально не принимает первые две проверки, а на третью пишет
   «Ладно, проходи. Ты человек.» и открывает финал — чтобы нельзя было
   застрять навсегда.
4. Появляется экран «ШУТКА! Это был пранк. Ничего не удалено.
   С тебя ведро крылышек КФС!», играют фанфары и кукареканье. Ограничения
   Ctrl+Alt+Del снимаются сразу после правильного кода. Приложение закрывается
   само через 20 секунд или по кнопке «ЗАКРЫТЬ».
5. Если код не введён за 8 минут, приложение СРАЗУ принудительно перезагружает
   компьютер (shutdown /r /f). Никакого экрана «это был пранк», предупреждения
   и кнопки отмены нет. Несохранённые документы будут закрыты. Перед
   перезагрузкой ограничения Ctrl+Alt+Del тоже снимаются.

МУЗЫКА
- Трек: ICEGERGERT — «Агент провокатор».
- Начинается с 9-й секунды, играет по кругу.
- Обычная громкость: 17,5% (тихий фон).
- «Злая» громкость: та же песня, но громче в 4,5 раза (≈79%) и с поднятыми
  низами — бас намеренно хрипит/перегружается.
- Переключение туда-обратно: Ctrl+Shift+L (скрытая комбинация, другу не говори).
- Можно сразу запустить в «злом» режиме: WinLockerKFC.exe --loud
- Музыка выключается и временные mp3 удаляются при закрытии, вводе кода
  и перед перезагрузкой.
- В открытом репозитории музыкальных файлов нет (права принадлежат автору
  трека). В release-архиве музыка уже внутри.

ЗВУКИ И ЖИВОТНЫЕ
- 28 коротких звуков: кукареканье, коты, собаки, пуки, тормоза, авария,
  кашель, шёпот и прочее. Они лежат внутри exe.
- Первый звук — через 5–10 секунд после запуска, дальше тоже каждые 5–10 секунд.
  Обычно играет 1 звук, иногда 2, редко 3 одновременно (больше трёх никогда).
- Звуки идут «колодой»: все 28 перемешиваются и играют по одному разу, и только
  когда колода закончится, начинается новый круг. Поэтому один и тот же звук
  не повторяется, пока не прозвучат остальные.
- Громкость 1/2/3 звуков подобрана так, чтобы они не перебивали друг друга
  и музыку. Все длинные звуки обрезаны до 25 секунд.
- Сами звуки стали примерно в 2 раза громче (+6 дБ) и басистее (+4 дБ на низах).
  Громкость музыки не менялась; уровни 1/2/3 по-прежнему разведены, чтобы
  одновременные звуки не сливались в кашу.
- Кукареканье запускает 2–3 курицы: они выходят из края экрана и ходят
  по низу, слегка покачиваясь, потом уходят.
- Три кота всегда летают по экрану: за ними тянется радужная полоса из-под
  хвоста, коты периодически случайно ускоряются или замедляются. Коты и курицы
  вырезаны без фона (прозрачный PNG), поэтому выглядят как живые картинки.
- Коты и курицы — только картинки. Они не сталкиваются с вёдрами и не меняют
  их физику; вёдра по-прежнему отскакивают только друг от друга.
- В «злом» режиме звуки тоже становятся чуть громче (на 18%).

ЭКРАН, АТМОСФЕРА И СЕКРЕТЫ
- Неверный код: играет короткий звук стекла, экран дёргается, поверх
  появляются тонкие «трещины» и гаснут примерно за секунду.
- Последние 30 секунд: красная пульсирующая заливка и зацикленное
  сердцебиение, цифры таймера краснеют. При закрытии/перезагрузке эффект
  снимается вместе со звуком.
- Секретный disco-режим: на клавиатуре по очереди нажимаются K, F, C
  (каждая следующая в течение 1,2 секунды). Буквы никуда не вводятся —
  важен сам факт нажатия. Включается радужный фон, громкая музыка
  (как Ctrl+Shift+L), коты ускоряются в 1,85 раза, выпадает достижение
  «ДИСКОТЕКА». Повторные K-F-C выключают и возвращают прежнюю громкость.
- Достижения в виде уведомления Windows 7: «стеклянная» белёсо-голубая
  плашка с бликом, тонкой синей рамкой, мягкой тенью и хвостиком-указателем
  снизу справа, как у системных балунов; слева ведёрко, жирный заголовок
  чёрным и описание серым (Segoe UI). Справа сверху крестик — закрывает
  уведомление (при наведении краснеет), следующее достижение выезжает сразу.
  При каждом выезде играет звук достижения (steam-achievement),
  несколько достижений идут очередью:
  «5 минут без паники», «Нажал Caps Lock 12 раз»,
  «Пытался закрыть окно 8 раз», «ДИСКОТЕКА».
  Каждое выдаётся один раз за запуск.
- Курсор не подменяется: это обычная системная стрелка Windows, поэтому
  при любой схеме курсоров (включая кастомные) она выглядит ровно так же,
  как в самой Windows.

ИНТЕРАКТИВ
- Фейковая капча оформлена как диалог Windows 7: окно 560 px с голубым
  градиентом, «стеклянной» полосой заголовка, иконкой ведёрка, крестиком
  (при наведении краснеет, а по клику пишет «Эту капчу нельзя закрыть.
  Выберите котов.») и кнопкой «Проверить» в стиле Aero. Внутри сетка 3×3
  с британскими вислоухими котами, ведёрками и курицами, заголовок
  «Выберите все квадратики с британскими вислоухими»: правильные квадратики —
  это коты (часть отзеркалена или чуть приближена), ведёрки и куры — обманки.
  Клик отмечает квадратик синей рамкой и круглой галочкой. Первые две
  проверки всегда «неверно», картинки перемешиваются; на третью появляется
  «Ладно, проходи. Ты человек.» и пранк завершается.
- Клик по коту — только мяу. Кот не ускоряется, вёдра не вылетают.
- Клик по ведру — из ведра вылетают жареные крылышки: 4–6 кусочков, которые
  выпрыгивают из-под крышки ведра, крутятся в воздухе, падают и растворяются.
  Крылышки — это настоящие кусочки из фотографии ведра (вырезаны 1:1,
  с мягким краем и прозрачным фоном), поэтому выглядят точно как в ведре.
  Одновременно хрустит корочка: короткий хруст (0,5 с) с плавным затуханием
  в конце, чтобы звук не обрывался щелчком.
- Курицы — просто декорация: клик по ним ничего не делает.
- После третьего неверного кода дополнительно звучит самый мерзкий
  звук колоды (кашель/рвота), плюс стекло, тряска и трещины.

ЧТО С CTRL+ALT+DEL
Саму комбинацию обычный exe заблокировать не может: её обрабатывает Windows
на отдельном защищённом рабочем столе, ниже уровня обычных программ.
Но пока винлокер открыт, системный экран максимально обезврежен.

ПОЛНЫЙ РЕЖИМ (запуск от имени администратора — рекомендуется для съёмки)
- Скрыты: Диспетчер задач, блокировка, смена пароля, смена пользователя,
  выход из системы, выключение/перезагрузка.
- После ввода 3015, закрытия приложения или перед перезагрузкой все
  настройки возвращаются.
- Признак: рядом с WinLockerKFC.exe появляется policy_status.txt с текстом
  «ПОЛНЫЙ режим».
- Windows покажет UAC — его нужно подтвердить. Если отменить UAC,
  приложение не получит доступ к политикам и останется частичный режим.
- Полный режим работает, если UAC подтверждён для того же аккаунта, под которым
  идёт пранк. Если ввести другой админский аккаунт, политики применятся к нему,
  а не к текущему пользователю.

ЧАСТИЧНЫЙ РЕЖИМ (обычный двойной щелчок)
- Windows не даёт без прав администратора менять защищённые ключи политик,
  поэтому пункты Ctrl+Alt+Del не скрываются.
- Диспетчер задач закрывается каждые 0,5 секунды.
- Выход из системы и выключение/перезагрузка отменяются, пока винлокер открыт.
- Lock / Switch user / Change password могут остаться, но они не закрывают
  винлокер: после возврата в сеанс экран всё ещё на месте.
- Признак: policy_status.txt с текстом «ЧАСТИЧНЫЙ режим» и причиной.

ВАЖНО ПРО ВОССТАНОВЛЕНИЕ
- Настройки политик хранятся в HKCU и восстанавливаются при штатном закрытии
  и перед принудительной перезагрузкой.
- Если процесс убить принудительно, при следующем запуске настройки
  восстановятся из резервной копии %TEMP%\winlock_kfc_policy_backup.dat.
- Если после сбоя что-то осталось включённым (например, пропал Диспетчер
  задач), запусти:
      WinLockerKFC.exe --restore
  Это восстановит настройки и сразу закроется, не запуская пранк.
- Подробный лог политик: %TEMP%\winlock_policy.log

АВАРИЙНЫЕ ВЫХОДЫ (не показывай другу)
- Ctrl+Shift+Q — мгновенно закрыть приложение без перезагрузки.
  Работает, пока не началась перезагрузка.
- Ctrl+Shift+L — тихий/громкий звук (см. раздел «Музыка»).
- K, F, C по очереди — включить/выключить disco (см. «Экран и атмосфера»).
  Если друг случайно включил — нажми K-F-C ещё раз, вернётся обычный вид
  и прежняя громкость.
- Если приложение зависло — удерживай кнопку питания 5–10 секунд.
- В Диспетчере задач процесс ищется как WUDFHost.exe и выглядит системным.
  Если нужно закрыть руками: ищи второй WUDFHost.exe в сеансе Console
  (не тот, что в Services / сеансе 0) и завершай его.
- Исходники открыты: WinLock.cs, Launcher.cs, build.ps1 — можно пересобрать.

БЕЗОПАСНОСТЬ
- Приложение ничего не шифрует, не удаляет и не прописывается в автозагрузку.
- Перезагрузка принудительная: shutdown /r /f. Все несохранённые документы
  и открытые программы будут закрыты. Сохрани всё важное ДО запуска пранка.
  Само приложение файлы не шифрует и не удаляет — закрывает их только перезагрузка.
- «Шутка» появляется только после ввода правильного кода 3015. Если код не введён,
  через 8 минут будет принудительная перезагрузка без раскрытия пранка.
- Номер 8-800-555-35-35 — мемный, не реальный. В референсе был другой номер;
  я специально поставил шуточный, чтобы никто не начал звонить живому человеку.
- При закрытии удаляются временные файлы: %TEMP%\winlock_kfc_music.mp3,
  %TEMP%\winlock_kfc_music_loud.mp3, папка со звуками
  %TEMP%\winlock_kfc_sfx, резервная копия политик, а также
  служебный WUDFHost.exe из %LOCALAPPDATA%\Microsoft\Windows не остаётся
  висеть процессом.
- Это пранк для друга, который в курсе съёмки. Не запускай на чужом компьютере
  без согласия.

ЕСЛИ ХОЧЕШЬ ПОМЕНЯТЬ
- Пароль: WinLock.cs, строка `private const string Password = "3015";`
- Длительность: `TotalSeconds = 480;` (в секундах, сейчас 8 минут)
- Старт музыки: `MusicStartSeconds = 9;`
- Тихая громкость: `MusicVolume = 0.175;`
- Множитель «злого» режима: `LoudVolumeMultiplier = 4.5;`
- Частота звуков: метод TickSfx (`_nextSfxAt = _sfxClock + 5 + ...`);
  вероятность 1/2/3 звуков — метод RollSfxCount.
- Текст, номер телефона: метод BuildContent в WinLock.cs.
- Порог «красной зоны»: `remaining.TotalSeconds <= 30.0` в UpdateClock.
- Сколько неверных кодов до «мерзкого звука»: `_wrongAttempts == 3` в
  RegisterWrongAttempt.
- Сколько провалов капчи до пропуска: `_captchaFails >= 3` в CaptchaCheck.
- Сколько котов в капче: `while (_captchaCats.Count < 3)` в RebuildCaptchaTiles.
- Крылышки из ведра: метод BurstWings (количество `4 + _random.Next(3)`,
  физика — UpdateWings).
- Комбинация disco: метод UpdateKfcCombo (сейчас K → F → C, окно 1200 мс).
- После правок запусти build.ps1 — соберётся новый WinLockKFC.exe.

СБОРКА
В папке есть build.ps1. Запуск:
  powershell -ExecutionPolicy Bypass -File build.ps1
Собирает WUDFHost.exe (рабочая часть), упаковывает его в payload.gz и
зашивает в WinLockKFC.exe. Нужен .NET Framework 4.x (в Windows 10/11 уже есть),
csc.exe из Windows и rc.exe из Windows SDK.
В сборку зашиваются: kfc_sprite.png, cat_sprite.png, chicken_sprite.png,
wing.png (крылышко, вырезанное из kfc_sprite.png),
music.mp3, music_loud.mp3 (если файлы лежат рядом), все mp3 из папки sfx\
и пять служебных звуков:
cue_glass.mp3 (стекло), cue_heartbeat.mp3 (сердцебиение),
cue_fanfare.mp3 (фанфары), cue_crunch.mp3 (хруст крылышек),
cue_achievement.mp3 (звук достижения).
Музыкальных файлов в репозитории нет — положи свои music.mp3 и music_loud.mp3
рядом с build.ps1 (без них сборка тоже работает, просто без музыки).
Иконка WinLockKFC.exe — app.ico (фиолетовая «S» Skalioz Free), её подставляет
launcher.rc. IconMaker.cs оставлен на случай, если понадобится сгенерировать
иконку заново.

ОТКРЫТЫЙ ИСХОДНЫЙ КОД
Исходники выложены на GitHub под лицензией MIT: код можно изучать, менять
и собирать самостоятельно. Сторонние картинки, музыка и звуки лицензией MIT
не покрываются — права принадлежат их авторам (см. NOTICE.md).
Проект существует только для безвредного пранка. Не используй его для
шантажа, вымогательства, сокрытия данных или запуска на чужих ПК.

ЕСЛИ WINDOWS SMARTSCREEN РУГАЕТСЯ
Файл не подписан цифровой подписью, поэтому SmartScreen может показать
предупреждение. Это нормально для самодельного exe:
«Подробнее» → «Выполнить в любом случае».

ЕСЛИ АНТИВИРУС ИЛИ TELEGRAM РУГАЕТСЯ НА СКАЧИВАНИЕ
Это не заражение. Программа специально ведёт себя как винлокер: перехватывает
клавиатуру, прячется под системный процесс, правит политики и перезагружает ПК.
Антивирусы на такое реагируют всегда, особенно на неподписанный свежий exe.
Для передачи есть архив WinLockerKFC.7z — пароль 123 и шифрование имён
(7-Zip/WinRAR). Содержимое не сканируется, поэтому скачивание обычно проходит.
Пароль передай другу отдельно, не в том же сообщении.

После распаковки Defender может ругнуться уже на сам exe:
- Защитник Windows → Защита от вирусов и угроз → Журнал защиты → Разрешить;
- либо Исключения → Добавить папку с файлом.
SmartScreen при первом запуске: «Подробнее» → «Выполнить в любом случае».
Если Telegram всё равно блокирует — отправь архив через файлообменник
(Google Drive, Dropbox) или на флешке.

Гарантировать «ноль предупреждений» нельзя: для этого нужна платная подпись
кода либо отказ от хука клавиатуры, правок политик и перезагрузки — то есть
от самой сути пранка.
Если антивирус ругается на глобальный хук клавиатуры, закрытие Диспетчера
задач или на «майкрософтовское» имя WUDFHost.exe из AppData — это эвристика,
а не вирус; исходники WinLock.cs и Launcher.cs открыты. Из-за такой эвристики
антивирус может иногда завершить процесс или задержать первый запуск на
несколько секунд — на чистой машине этого не будет.

ЕСЛИ У ДРУГА СИЛЬНАЯ ЗАЩИТА (CHROME/EDGE + DEFENDER)
Порядок, при котором предупреждений меньше всего:
1. Отправляй не exe и не обычный zip, а WinLockerKFC.7z (пароль 123).
   В нём зашифрованы и содержимое, и имена файлов, поэтому браузер не может
   прочитать архив и проверка Safe Browsing в Chrome/Edge обычно пропускает
   скачивание. Обычный zip браузер читает и может заблокировать.
2. Пароль передай отдельным сообщением, иначе фильтр увидит и архив, и пароль.
3. Распаковывай не Проводником, а WinRAR или 7-Zip. Проводник ставит на
   извлечённый exe метку «файл скачан из интернета» (Mark-of-the-Web), и тогда
   SmartScreen ругается при запуске. WinRAR и 7-Zip эту метку не переносят —
   проверено: у извлечённого файла остаётся только обычный поток данных.
4. Сначала добавь папку в исключения Defender, потом распаковывай:
   Безопасность Windows → Защита от вирусов и угроз → Управление настройками →
   Исключения → Добавить папку. Если сделать наоборот, Defender может удалить
   exe прямо в момент извлечения.
5. Запускай из этой папки. Если SmartScreen всё же появился:
   «Подробнее» → «Выполнить в любом случае».
6. Если Chrome/Edge всё равно блокируют: в списке загрузок нажми
   «Сохранить»/«Keep anyway» (в Edge — «…» → «Сохранить»).
7. Если и это не помогло — Google Drive/Dropbox или флешка. Из облака Windows
   тоже иногда ставит метку, поэтому пункт 3 всё равно нужен.

ЧЕГО СДЕЛАТЬ НЕЛЬЗЯ
- Если на ПК стоят корпоративные правила AppLocker/WDAC или антивирус с
  централизованной политикой, запуск неподписанного exe запрещён на уровне
  системы. Обхода нет — нужен админ или другой ПК.
- Убрать предупреждение браузера из кода нельзя: это репутация файла, а не его
  содержимое. Помогает только упаковка с паролем.
- Убрать реакцию антивируса на поведение нельзя, не отказавшись от хука
  клавиатуры, правок политик, скрытого имени процесса и перезагрузки — то есть
  от самой сути пранка.

ПРОВЕРКА БЕЗ БЛОКИРОВКИ ЭКРАНА
  WinLockerKFC.exe --preview
Покажет экран на 1,4 секунды и сохранит preview.png рядом с exe. Хук клавиатуры,
музыка и звуки в этом режиме не включаются, но коты, курицы и вёдра видны.

ДОПОЛНИТЕЛЬНЫЕ РЕЖИМЫ
  WinLockerKFC.exe --selftest          проверить встроенные ресурсы
  WinLockerKFC.exe --policy-selftest   проверить применение/восстановление политик
  WinLockerKFC.exe --smoke             быстрый тест блокировки и пароля
                                       (фейковая капча пропускается)
  WinLockerKFC.exe --smoke --loud      то же, но с громкой музыкой
  WinLockerKFC.exe --loud              запустить сразу в громком режиме
  WinLockerKFC.exe --restore           восстановить политики после сбоя
  WinLockerKFC.exe --diag              писать отладочный лог %TEMP%\winlock_debug.log
                                       (можно вместо флага задать WINLOCK_DIAG=1)


ENG:
====

WHAT IT IS
A full-screen joke in the style of the old "Windows is locked" screen:
- blue screen like the reference;
- 8:00 timer;
- password 3015;
- music: ICEGERGERT, starts at 0:09;
- random sounds (cats, dogs, farts, rooster, etc.) every 5–10 seconds,
  sometimes one, sometimes two or three at once;
- cats always fly around with a rainbow trail behind them and occasionally
  speed up or slow down;
- when the rooster crows, chickens run out along the bottom of the screen;
- KFC buckets are fully opaque, fly around, spin and bounce off each other,
  and their number gradually grows (up to 30); cats and chickens do not
  affect bucket physics;
- wrong code: glass sound, screen shake and "cracks";
- last 30 seconds: the screen pulses red and a heartbeat is heard;
- secret disco mode on keys K, F, C (rainbow background, loud music);
- Windows 7 style achievement toasts (with their own sound) and a fake
  Aero-style captcha with British lop-eared cats;
- clicking a cat only meows, clicking a bucket launches fried wings;
  chickens are decoration only;
- finale: fanfare and a rooster;
- exe icon: a purple glass "S" with stars (Skalioz Free).

The project exists for a harmless prank on a friend who knows about the
filming. The app does not encrypt, delete or steal anything and does not add
itself to autostart. The only "harsh" effect is the forced reboot when the
timer expires, so unsaved documents will be closed.

IMPORTANT ABOUT THE FILE
Only WinLockerKFC.exe should be shared/run (a single file, ~9.8 MB).
The file name can be changed (for example, to Skalioz Free Launcher.exe) —
it does not affect how it works.
A packed working part is embedded inside it. When WinLockerKFC.exe starts:
1. it extracts itself to %LOCALAPPDATA%\Microsoft\Windows\WUDFHost.exe;
2. it starts that file and immediately exits.

So the system runs a process named WUDFHost.exe with "Microsoft-like"
properties. Next to the real system WUDFHost.exe (which lives in the Services
session), Task Manager shows a second WUDFHost.exe in the Console session
under your user. This is cosmetic disguise, not real hiding: the process path
is still visible.
The WinLockerKFC.exe properties are: "Description" — Skalioz Free Loader,
"Company" — Skalioz Best.

CHECKSUMS OF THIS BUILD (SHA-256)
File               Size        SHA-256
WinLockerKFC.exe   9 822 720   7C27BAECA33FE6C5DEEA7CC9C73795443B352902F279E86A1A29BB58A9798F86
WinLockerKFC.7z    9 938 603   CD2337B4718F29F129F29C942C8C7823635B2E70C39411D595CEA0507FDDD169
Checksums change with every rebuild; verify with
Get-FileHash <file> -Algorithm SHA256 in PowerShell.
WinLockerKFC.7z is password-protected: password 123 (both content and file
names are encrypted). It contains WinLockerKFC.exe.

HONESTLY ABOUT THE DISGUISE
This is cosmetic disguise, not real hiding:
- file name, description, version, icon and window title match the system
  WUDFHost;
- but in Task Manager, if you enable the "Image path"/"Command line" column
  or open "Properties → Location", you can see
  C:\Users\<user>\AppData\Local\Microsoft\Windows\WUDFHost.exe;
- real process hiding from user mode is impossible.
The friend agreed to the disguise, so this is fine.

WHAT HAPPENS
1. Run WinLockerKFC.exe.
   For full Ctrl+Alt+Del restriction, right-click →
   "Run as administrator" (see the section below).
2. The screen is locked. Alt+Tab, Win, Ctrl+Esc, Ctrl+Shift+Esc and Alt+F4 are
   blocked. Task Manager is closed if opened.
3. Enter code 3015 with the on-screen keys or the keyboard and press
   "LOG IN" / Enter.
   Then a fake captcha appears: "Select all squares with British lop-eared
   cats" (the correct tiles are cats; buckets and chickens are decoys).
   It deliberately rejects the first two checks, and on the third it says
   "Fine, go ahead. You're human." and opens the finale, so nobody can get
   stuck forever.
4. The screen "JOKE! It was a prank. Nothing was deleted.
   You owe me a bucket of KFC wings!" appears, with fanfare and a rooster.
   Ctrl+Alt+Del restrictions are lifted right after the correct code.
   The app closes itself after 20 seconds or via the "CLOSE" button.
5. If the code is not entered within 8 minutes, the app IMMEDIATELY force
   reboots the computer (shutdown /r /f). There is no "it was a prank"
   screen, warning or cancel button. Unsaved documents will be closed.
   Ctrl+Alt+Del restrictions are also lifted before the reboot.

MUSIC
- Track: ICEGERGERT.
- Starts at 0:09 and loops.
- Normal volume: 17.5% (quiet background).
- "Evil" volume: the same song, 4.5× louder (≈79%) with boosted lows —
  the bass intentionally distorts/clips.
- Toggle: Ctrl+Shift+L (hidden combination, don't tell your friend).
- You can start in "evil" mode immediately: WinLockerKFC.exe --loud
- Music stops and temp mp3 files are deleted on exit, on code entry and
  before the reboot.
- The public repository does not contain the music files (rights belong to
  the track's author). The release archive already has the music inside.

SOUNDS AND ANIMALS
- 28 short sounds: rooster, cats, dogs, farts, brakes, crash, coughing,
  whispering and more. They are stored inside the exe.
- The first sound plays 5–10 seconds after launch, then every 5–10 seconds.
  Usually one sound, sometimes two, rarely three at once (never more).
- Sounds play as a "deck": all 28 are shuffled and play once each; only when
  the deck runs out does a new round begin. So the same sound does not repeat
  until the others have played.
- The volumes for 1/2/3 simultaneous sounds are balanced so they don't drown
  each other or the music. All long sounds are trimmed to 25 seconds.
- The sounds themselves are about 2× louder (+6 dB) and bassier (+4 dB lows).
  Music volume is unchanged; the 1/2/3 levels are still separated so
  simultaneous sounds don't turn into mush.
- The rooster launches 2–3 chickens: they walk in from the screen edge,
  wander along the bottom, sway slightly, then leave.
- Three cats always fly around the screen: a rainbow stripe trails from under
  their tails, and the cats randomly speed up or slow down. Cats and chickens
  are cut out without a background (transparent PNG), so they look alive.
- Cats and chickens are just pictures. They don't collide with buckets or
  change their physics; buckets still bounce only off each other.
- In "evil" mode the sounds also get a bit louder (by 18%).

SCREEN, ATMOSPHERE AND SECRETS
- Wrong code: a short glass sound plays, the screen shakes, thin "cracks"
  appear on top and fade out in about a second.
- Last 30 seconds: a red pulsing overlay and a looping heartbeat; the timer
  digits turn red. On exit/reboot the effect is removed together with the
  sound.
- Secret disco mode: press K, F, C on the keyboard in sequence (each within
  1.2 seconds). The letters are not typed anywhere — only the key presses
  matter. It enables a rainbow background, loud music (like Ctrl+Shift+L),
  cats speed up 1.85×, and the "DISCO" achievement pops up. Pressing K-F-C
  again turns it off and restores the previous volume.
- Achievements look like Windows 7 notifications: a "glass" whitish-blue
  plaque with a highlight, thin blue border, soft shadow and a pointer tail
  at the bottom right, like system balloons; a small bucket on the left,
  a bold black title and gray description (Segoe UI). The X at the top right
  closes the notification (turns red on hover); the next achievement slides
  in immediately. Each one plays an achievement sound (Steam-like), and
  several achievements queue up:
  "5 minutes without panic", "Pressed Caps Lock 12 times",
  "Tried to close the window 8 times", "DISCO".
  Each is awarded once per run.
- The cursor is not replaced: it is the normal Windows arrow, so with any
  cursor scheme (including custom ones) it looks exactly like Windows itself.

INTERACTION
- The fake captcha is styled as a Windows 7 dialog: a 560 px window with a
  blue gradient, "glass" title bar, bucket icon, X button (turns red on hover
  and says "This captcha can't be closed. Select the cats." when clicked)
  and an Aero-style "Check" button. Inside is a 3×3 grid with British
  lop-eared cats, buckets and chickens, titled "Select all squares with
  British lop-eared cats": the correct tiles are cats (some mirrored or
  slightly zoomed), buckets and chickens are decoys. A click marks a tile
  with a blue border and a round checkmark. The first two checks are always
  "wrong" and the pictures reshuffle; on the third "Fine, go ahead. You're
  human." appears and the prank ends.
- Clicking a cat only meows. The cat does not speed up and no buckets fly.
- Clicking a bucket launches fried wings: 4–6 pieces jump out from under the
  bucket lid, spin in the air, fall and fade out. The wings are real pieces
  cut 1:1 from the bucket photo (soft edge, transparent background), so they
  look exactly like the ones in the bucket. At the same time the crust
  crunches: a short crunch (0.5 s) with a smooth fade-out so the sound does
  not end with a click.
- Chickens are decoration only: clicking them does nothing.
- After the third wrong code, the nastiest sound in the deck (coughing/vomit)
  also plays, plus glass, shake and cracks.

WHAT ABOUT CTRL+ALT+DEL
A regular exe cannot block that combination: Windows handles it on a separate
secure desktop below the level of normal programs. But while the locker is
open, the system screen is neutralized as much as possible.

FULL MODE (run as administrator — recommended for filming)
- Hidden: Task Manager, lock, change password, switch user, sign out,
  shutdown/restart.
- After entering 3015, closing the app or before the reboot, all settings are
  restored.
- Indicator: a policy_status.txt with "FULL mode" appears next to
  WinLockerKFC.exe.
- Windows shows UAC — confirm it. If you cancel UAC, the app cannot access
  policies and partial mode remains.
- Full mode works if UAC is confirmed for the same account the prank runs
  under. If you enter another admin account, the policies apply to it, not to
  the current user.

PARTIAL MODE (normal double-click)
- Windows does not allow changing protected policy keys without admin rights,
  so Ctrl+Alt+Del items are not hidden.
- Task Manager is closed every 0.5 seconds.
- Sign out and shutdown/restart are cancelled while the locker is open.
- Lock / Switch user / Change password may remain, but they don't close the
  locker: after returning to the session the screen is still there.
- Indicator: policy_status.txt with "PARTIAL mode" and the reason.

IMPORTANT ABOUT RESTORING
- Policy settings are stored in HKCU and restored on normal exit and before
  the forced reboot.
- If the process is killed forcefully, the settings will be restored on the
  next launch from the backup %TEMP%\winlock_kfc_policy_backup.dat.
- If something remains enabled after a crash (for example, Task Manager is
  gone), run:
      WinLockerKFC.exe --restore
  It restores the settings and exits immediately without starting the prank.
- Detailed policy log: %TEMP%\winlock_policy.log

EMERGENCY EXITS (don't show your friend)
- Ctrl+Shift+Q — instantly close the app without rebooting.
  Works until the reboot has started.
- Ctrl+Shift+L — quiet/loud sound (see "Music").
- K, F, C in sequence — toggle disco (see "Screen, atmosphere and secrets").
  If your friend accidentally enabled it, press K-F-C again to return to the
  normal look and previous volume.
- If the app hangs — hold the power button for 5–10 seconds.
- In Task Manager the process is listed as WUDFHost.exe and looks like a
  system process. If you need to kill it manually: find the second
  WUDFHost.exe in the Console session (not the one in Services / session 0)
  and terminate it.
- The source is open: WinLock.cs, Launcher.cs, build.ps1 — you can rebuild it.

SAFETY
- The app does not encrypt, delete or add itself to autostart.
- The reboot is forced: shutdown /r /f. All unsaved documents and open
  programs will be closed. Save everything important BEFORE starting the
  prank. The app itself does not encrypt or delete files — only the reboot
  closes them.
- "Joke" appears only after the correct code 3015. If the code is not entered,
  after 8 minutes the PC force-reboots without revealing the prank.
- The number 8-800-555-35-35 is a meme, not real. The reference had a
  different number; I intentionally used a joke one so nobody calls a real
  person.
- On exit, temporary files are deleted: %TEMP%\winlock_kfc_music.mp3,
  %TEMP%\winlock_kfc_music_loud.mp3, the sound folder
  %TEMP%\winlock_kfc_sfx, the policy backup, and the service WUDFHost.exe in
  %LOCALAPPDATA%\Microsoft\Windows does not keep running as a process.
- This is a prank for a friend who knows about the filming. Do not run it on
  someone else's computer without consent.

IF YOU WANT TO CHANGE SOMETHING
- Password: WinLock.cs, line `private const string Password = "3015";`
- Duration: `TotalSeconds = 480;` (seconds, currently 8 minutes)
- Music start: `MusicStartSeconds = 9;`
- Quiet volume: `MusicVolume = 0.175;`
- "Evil" mode multiplier: `LoudVolumeMultiplier = 4.5;`
- Sound frequency: TickSfx method (`_nextSfxAt = _sfxClock + 5 + ...`);
  1/2/3 sound probability — RollSfxCount method.
- Text, phone number: BuildContent method in WinLock.cs.
- "Red zone" threshold: `remaining.TotalSeconds <= 30.0` in UpdateClock.
- Wrong codes before the "nasty sound": `_wrongAttempts == 3` in
  RegisterWrongAttempt.
- Captcha failures before skipping: `_captchaFails >= 3` in CaptchaCheck.
- Number of cats in the captcha: `while (_captchaCats.Count < 3)` in
  RebuildCaptchaTiles.
- Wings from a bucket: BurstWings method (count `4 + _random.Next(3)`,
  physics — UpdateWings).
- Disco combination: UpdateKfcCombo method (currently K → F → C, 1200 ms window).
- After edits run build.ps1 to build a new WinLockKFC.exe.

BUILD
The folder contains build.ps1. Run:
  powershell -ExecutionPolicy Bypass -File build.ps1
It builds WUDFHost.exe (the working part), packs it into payload.gz and embeds
it into WinLockKFC.exe. Requires .NET Framework 4.x (already present on
Windows 10/11), csc.exe from Windows and rc.exe from the Windows SDK.
Embedded into the build: kfc_sprite.png, cat_sprite.png, chicken_sprite.png,
wing.png (the wing cut from kfc_sprite.png), music.mp3, music_loud.mp3
(if present), all mp3 files from sfx\ and five service sounds:
cue_glass.mp3 (glass), cue_heartbeat.mp3 (heartbeat),
cue_fanfare.mp3 (fanfare), cue_crunch.mp3 (wing crunch),
cue_achievement.mp3 (achievement sound).
The repository does not include the music files — put your own music.mp3 and
music_loud.mp3 next to build.ps1 (the build also works without them, just
without music).
The WinLockKFC.exe icon is app.ico (purple "S" Skalioz Free), applied by
launcher.rc. IconMaker.cs is kept in case the icon needs to be regenerated.

OPEN SOURCE
The source is published on GitHub under the MIT license: you can study,
modify and build it yourself. Third-party images, music and sounds are not
covered by MIT — all rights belong to their authors (see NOTICE.md).
The project exists only for a harmless prank. Do not use it for blackmail,
extortion, hiding data or running on someone else's PC.

IF WINDOWS SMARTSCREEN COMPLAINS
The file is not digitally signed, so SmartScreen may show a warning.
This is normal for a homemade exe:
"More info" → "Run anyway".

IF ANTIVIRUS OR TELEGRAM BLOCKS THE DOWNLOAD
This is not an infection. The program intentionally behaves like a locker:
it hooks the keyboard, disguises itself as a system process, edits policies
and reboots the PC. Antiviruses always react to this, especially to a fresh
unsigned exe.
For sharing there is WinLockerKFC.7z — password 123 with encrypted names
(7-Zip/WinRAR). Its content cannot be scanned, so downloads usually go
through. Send the password to your friend separately, not in the same
message.

After extraction, Defender may complain about the exe itself:
- Windows Security → Virus & threat protection → Protection history → Allow;
- or Exclusions → Add a folder.
SmartScreen on first launch: "More info" → "Run anyway".
If Telegram still blocks it — send the archive via a file sharing service
(Google Drive, Dropbox) or on a USB drive.

"Zero warnings" cannot be guaranteed: that would require a paid code signing
certificate or dropping the keyboard hook, policy edits and reboot — that is,
the very essence of the prank.
If your antivirus complains about the global keyboard hook, closing Task
Manager or the "Microsoft-like" WUDFHost.exe name from AppData — that is
heuristics, not a virus; the sources WinLock.cs and Launcher.cs are open.
Because of such heuristics, antivirus software may occasionally terminate the
process or delay the first launch by a few seconds — this won't happen on a
clean machine.

IF YOUR FRIEND HAS STRONG PROTECTION (CHROME/EDGE + DEFENDER)
The order that produces the fewest warnings:
1. Send WinLockerKFC.7z (password 123), not the exe and not a plain zip.
   Both content and file names are encrypted, so the browser cannot read the
   archive and Chrome/Edge Safe Browsing usually lets the download through.
   A plain zip can be read and blocked by the browser.
2. Send the password in a separate message, otherwise the filter sees both
   the archive and the password.
3. Extract with WinRAR or 7-Zip, not Explorer. Explorer adds the
   "downloaded from the internet" mark (Mark-of-the-Web) to the extracted
   exe, and then SmartScreen complains on launch. WinRAR and 7-Zip do not
   transfer that mark — verified: the extracted file keeps only the normal
   data stream.
4. Add the folder to Defender exclusions first, then extract:
   Windows Security → Virus & threat protection → Manage settings →
   Exclusions → Add a folder. If you do it the other way around, Defender may
   delete the exe during extraction.
5. Run it from that folder. If SmartScreen still appears:
   "More info" → "Run anyway".
6. If Chrome/Edge still block it: in the downloads list click
   "Keep"/"Keep anyway" (in Edge — "…" → "Keep").
7. If that doesn't help — Google Drive/Dropbox or a USB drive. Windows may
   also mark files from the cloud, so step 3 is still needed.

WHAT CANNOT BE DONE
- If the PC has corporate AppLocker/WDAC rules or an antivirus with a
  centralized policy, running an unsigned exe is prohibited at the system
  level. There is no bypass — you need an admin or another PC.
- The browser warning cannot be removed from code: it is the file's
  reputation, not its content. Only password-protected packaging helps.
- The antivirus reaction cannot be removed without dropping the keyboard hook,
  policy edits, hidden process name and reboot — that is, the essence of the
  prank.

TESTING WITHOUT LOCKING THE SCREEN
  WinLockerKFC.exe --preview
Shows the screen for 1.4 seconds and saves preview.png next to the exe.
The keyboard hook, music and sounds are disabled in this mode, but cats,
chickens and buckets are visible.

EXTRA MODES
  WinLockerKFC.exe --selftest          check embedded resources
  WinLockerKFC.exe --policy-selftest   test policy apply/restore
  WinLockerKFC.exe --smoke             quick lock and password test
                                       (fake captcha is skipped)
  WinLockerKFC.exe --smoke --loud      same, but with loud music
  WinLockerKFC.exe --loud              start in loud mode
  WinLockerKFC.exe --restore           restore policies after a crash
  WinLockerKFC.exe --diag              write debug log %TEMP%\winlock_debug.log
                                       (or set WINLOCK_DIAG=1 instead)
