# NuGet pages (NUGET.md) — факты + сборка

Страница пакета на nuget.org — генерируемый артефакт. Правим **факты** (`packages/*.md`) и
шаблон в тулe, не выход.

## Как это работает

| Слой | Где |
|------|-----|
| Факты | `packages/<PackageId>.md` — заголовок (`package:`, `tagline:`, `project:` — необязательный путь к проекту для чужих раскладок) + шесть секций |
| Шаблон/сборка | `guiders-assist/scripts/build-nuget-readmes.csx` (`dotnet script`) |
| Выход | `src/<PackageId>/NUGET.md` — генерируемый, коммитится как `*.generated.md`-доки |
| Упаковка | `Directory.Build.targets` в корне: проект с `NUGET.md` пушит его как readme пакета; без него — корневой README репы |

## Секции (порядок фиксирован, все обязательны)

- `What you get` — поимённо: типы/механики; перечисления — подсписками
- `Quick start` — честный минимальный пример из реального API
- `Problem` — язык мира читателя (app config, layered settings), не внутренний контекст федерации
- `Solution` — что меняется + где обкатано (внутреннее = ссылки, не опора смысла)
- `When not to use` — честные границы пакета
- `Learn more` — ссылки

Подвал (MIT + ссылка на репо) штампуется тулом из `git remote origin` — в фактах его нет.

## Сборка

```bash
# из корня этой репы
dotnet script <путь-к-guiders-assist>/scripts/build-nuget-readmes.csx          # регенерация
dotnet script <путь-к-guiders-assist>/scripts/build-nuget-readmes.csx --check # проверка без записи
```

Тул публичный: для чужих раскладок — `--facts <папка>` и заголовок `project:` в факте
(fsproj/csproj/vbproj); подробности — в шапке скрипта.

Тул печатает статус по каждому пакету (generated/unchanged) и покрытие: кто без фактов
(те продолжают получать корневой README). Править можно всё что угодно и когда угодно —
правка факта или шаблона + пересборка.
