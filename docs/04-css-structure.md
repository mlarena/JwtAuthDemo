# Структура CSS-системы — Документация

## Обзор

CSS-система построена по принципу Atomic CSS (аналог Tailwind). Все стили разбиты на модули и подключаются через главный файл `site.css` с помощью `@import`.

## Структура файлов

```
wwwroot/css/
├── site.css                    # Главный файл — импортирует все модули
├── 0-settings/
│   ├── variables.css           # CSS-переменные (цвета, тени, отступы, шрифты)
│   └── reset.css               # Сброс стилей браузера, scrollbar, smooth scroll
├── 1-utilities/
│   ├── layout.css              # Flex, Grid, позиционирование, размеры, адаптивность
│   ├── spacing.css             # Padding, Margin, Gap (p-4, mt-2, gap-4 и т.д.)
│   ├── typography.css          # Размер шрифта, жирность, межстрочник, выравнивание
│   ├── colors.css              # Цвета текста, фона, бордеров через CSS-переменные
│   ├── borders.css             # Скругления, рамки, тени
│   └── interactions.css        # Курсор, transitions, opacity, transform
├── 2-components/
│   ├── buttons.css             # Кнопки (.btn, .btn-primary, .btn-danger, .btn-sm и т.д.)
│   ├── forms.css               # Поля ввода (.form-control, .form-group, .form-label, валидация)
│   ├── tables.css              # Таблицы (.table, .table-hover, .table-striped, badges)
│   ├── cards.css               # Карточки (.card, .stat-card, .detail-list, .danger-zone)
│   ├── modals.css              # Модальные окна
│   ├── alerts.css              # Уведомления (.alert-success, .alert-danger, toast)
│   ├── pagination.css          # Пагинация
│   └── breadcrumbs.css         # Хлебные крошки
├── 3-layouts/
│   ├── header.css              # Верхняя панель (хедер, поиск, аватар, тема)
│   ├── sidebar.css             # Левое меню (группы, сворачивание, мобильная версия)
│   └── main-content.css        # Основная область (page-header, content-grid, адаптивность)
└── 4-helpers/
    ├── animations.css          # Анимации (fadeIn, slideIn, spinner, skeleton)
    └── print.css               # Стили для печати
```

## Как работает подключение

Файл `site.css` — только импорты:
```css
@import url('0-settings/variables.css');
@import url('0-settings/reset.css');
@import url('1-utilities/layout.css');
/* ... остальные модули ... */
```

В `_Layout.cshtml` подключается один файл:
```html
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```

## CSS-переменные (темы)

Все цвета заданы через переменные в `variables.css`. Две темы:

**Светлая** (`:root`):
- `--bg-body: #f8fafc` — фон страницы
- `--bg-surface: #ffffff` — фон карточек/панелей
- `--text-primary: #0f172a` — основной текст
- `--text-secondary: #475569` — вторичный текст
- `--color-primary: #3b82f6` — акцентный цвет

**Тёмная** (`[data-theme="dark"]`):
- `--bg-body: #0f172a`
- `--bg-surface: #1e293b`
- `--text-primary: #f1f5f9`

Переключение — через атрибут `data-theme` на `<html>`. Сохраняется в `localStorage`.

## Как стили работают на Dashboard

Dashboard использует компонент `stat-card` и `content-grid`:

```html
<div class="content-grid cols-4 mb-6">
    <div class="stat-card">
        <div class="stat-icon primary"><i class="fas fa-users"></i></div>
        <div class="stat-label">Пользователи</div>
        <div class="stat-value">1,234</div>
    </div>
    <!-- ещё 3 карточки -->
</div>
```

- `.content-grid.cols-4` — CSS Grid с 4 колонками (адаптивно: 2 на планшете, 1 на мобильном)
- `.stat-card` — карточка с иконкой, лейблом и значением. При ховере — поднимается тень
- `.stat-icon.primary` — иконка с синим фоном (варианты: `.success`, `.danger`, `.warning`)

Карточки "Последняя активность" и "Статус системы" — обычные `.card`:
```html
<div class="card">
    <div class="card-header"><h3 class="card-title">Заголовок</h3></div>
    <div class="card-body">Контент</div>
</div>
```

## Как стили работают на CRUD-представлениях

### Index (список)

```html
<div class="page-header">
    <h1>Посты мониторинга</h1>
    <div class="page-actions">
        <a class="btn btn-primary"><i class="fas fa-plus"></i> Создать</a>
    </div>
</div>

<div class="table-responsive">
    <table class="table table-hover table-striped">
        <thead>...</thead>
        <tbody>...</tbody>
    </table>
</div>
```

- `.page-header` — flex-контейнер: заголовок слева, кнопки справа
- `.table-responsive` — обёртка с `overflow-x: auto` (горизонтальный скролл на мобильных)
- `.table` — базовая таблица с padding в ячейках
- `.table-hover` — подсветка строки при наведении
- `.table-striped` — зebra-раскраска (чётные строки серые)
- `.table-actions` — flex-контейнер для кнопок действий в строке
- `.badge-success` / `.badge-gray` — бейджи статуса

Кнопки действий — иконки в `.btn-ghost.btn-sm` (редактирование, просмотр) и `.btn-outline-danger.btn-sm` (удаление).

### Create / Edit (форма)

```html
<div class="card">
    <form asp-action="Create">
        <div class="card-body">
            <div class="form-group">
                <label class="form-label">Название <span class="required">*</span></label>
                <input class="form-control" />
                <span class="form-error"></span>
            </div>
            <div class="form-row">
                <!-- два поля в ряд -->
            </div>
        </div>
        <div class="card-footer">
            <a class="btn btn-secondary">Отмена</a>
            <button class="btn btn-primary"><i class="fas fa-save"></i> Создать</button>
        </div>
    </form>
</div>
```

- `.form-group` — flex-column контейнер: label + input + ошибка
- `.form-label` — лейбл поля
- `.form-control` — инпут/селект/текстареа (единый стиль)
- `.form-row` — Grid с 2 колонками (на мобильном — 1 колонка)
- `.form-error` — скрыт по умолчанию (`display: none`). Показывается только когда ASP.NET Core добавляет класс `field-validation-error`
- `.input-validation-error` — красная рамка на инпуте при ошибке валидации
- `.card-footer` — футер формы с кнопками (выровнен вправо)

**Важно для валидации:**
- jQuery + jQuery Validation + jQuery Unobtrusive подключены глобально в `_Layout.cshtml`
- Скрипты лежат локально в `wwwroot/lib/`
- Ошибки валидации появляются только при отправке формы (клиентская валидация)

### Details (просмотр)

```html
<div class="card">
    <div class="card-body" style="padding:0">
        <dl class="detail-list">
            <dt>Название</dt><dd>АДМС</dd>
            <dt>Описание</dt><dd>Автоматическая станция</dd>
        </dl>
    </div>
</div>
```

- `.detail-list` — Grid с 2 колонками: 140px (метка) + 1fr (значение)
- На мобильном — 1 колонка (метка сверху, значение снизу)
- Строки разделены `border-bottom`

### Delete (подтверждение)

```html
<div class="danger-zone mb-4">
    <div class="flex items-center gap-2 mb-2">
        <i class="fas fa-exclamation-triangle text-danger"></i>
        <h3>Подтверждение удаления</h3>
    </div>
    <p>Вы уверены?</p>
</div>

<div class="card mb-4">
    <div class="card-body" style="padding:0">
        <dl class="detail-list">...</dl>
    </div>
</div>

<form asp-action="Delete">
    <div class="flex gap-3">
        <a class="btn btn-secondary">Отмена</a>
        <button class="btn btn-danger"><i class="fas fa-trash"></i> Удалить</button>
    </div>
</form>
```

- `.danger-zone` — красная рамка + светло-красный фон
- Кнопка удаления — `.btn-danger` (красная)

## Sidebar (левое меню)

Структура меню:
```
.sidebar-nav
  ├── .sidebar-link (простая ссылка — Дашборд, О программе)
  └── .sidebar-group (сворачиваемая группа)
      ├── .sidebar-group-title (кликабельный заголовок с иконкой и стрелкой)
      └── .sidebar-group-items (контейнер ссылок)
          └── .sidebar-link (ссылки внутри группы)
```

- Группы сворачиваются по клику на заголовок
- Состояние сохраняется в `localStorage` (ключ `collapsedGroups`)
- Стрелка `.group-arrow` поворачивается на 90° при сворачивании
- Анимация через `max-height` + `opacity`
- Ссылки внутри группы имеют `padding-left: 2.5rem` (отступ от иконки группы)

**Мобильная версия (< 768px):**
- Сайдбар скрыт (`transform: translateX(-100%)`)
- Показывается по клику на бургер (класс `.show`)
- Overlay затемняет фон
- Бургер-кнопка видна только на мобильных

## Header (верхняя панель)

- `.header-left` — бургер + логотип
- `.header-right` — поиск + разделитель + переключатель темы + разделитель + аватар
- `.header-search` — скрыт на мобильных
- `.theme-toggle` — переключатель с иконками солнца/луны
- `.user-menu` — аватар + имя (обрезается с `...` если длинное)

## Кнопки (.btn)

Варианты:
| Класс | Описание |
|-------|----------|
| `.btn-primary` | Синяя кнопка (основное действие) |
| `.btn-secondary` | Серая кнопка (отмена, назад) |
| `.btn-danger` | Красная кнопка (удаление) |
| `.btn-success` | Зелёная кнопка |
| `.btn-warning` | Жёлтая кнопка |
| `.btn-outline-primary` | Контурная синяя |
| `.btn-outline-danger` | Контурная красная |
| `.btn-ghost` | Прозрачная (текстовая) |
| `.btn-sm` | Маленький размер |
| `.btn-lg` | Большой размер |
| `.btn-icon` | Круглая только с иконкой |
| `.btn-loading` | Состояние загрузки (спиннер) |

## Адаптивность

Брейкпоинты:
- `< 640px` — мобильный (1 колонка)
- `640-768px` — планшет (2 колонки)
- `> 768px` — десктоп (полный sidebar)

Утилиты: `.sm:flex`, `.md:grid-cols-2`, `.lg:grid-cols-4` и т.д.

## Доступность

- `:focus-visible` — яркая обводка на всех интерактивных элементах
- `aria-label` на кнопках
- `role="navigation"` на sidebar
- Контраст текста соответствует WCAG AA
