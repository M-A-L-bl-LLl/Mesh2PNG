# 🖼️ Mesh2PNG

<p align="center">
  <b>Рендер 3D объектов в прозрачный PNG прямо из Unity Editor</b><br>
  <b>Render 3D objects to transparent PNG right inside the Unity Editor</b>
</p>

---

## 🇷🇺 Русский

### О инструменте

**Mesh2PNG** — редакторный инструмент для создания спрайтов и иконок из 3D объектов. Открывается как обычное окно редактора, работает без Play Mode и без настройки сцены. Просто добавил объект, настроил ракурс и сохранил PNG с прозрачным фоном.

Прозрачность получается честная: объект рендерится дважды — на чёрном и белом фоне, затем альфа-канал восстанавливается из разницы. Это позволяет корректно передать полупрозрачные части (стекло, частицы, тонкие края).

### ✨ Возможности

- 🎨 **Прозрачный фон** без артефактов
- 📦 **Несколько объектов** в одном списке с индивидуальными настройками камеры и освещения
- 🎥 **Живое превью** — вращение перетаскиванием, зум колёсиком
- 🌲 **Иерархия** — скрывать дочерние объекты и фокусировать камеру на любом объеке иерархии
- 💡 **Два источника света** с настройкой цвета, яркости и направления
- 📐 **Произвольное разрешение** выходного PNG

### 📦 Установка

**Через Git URL**

В Unity: `Window → Package Manager → + → Add package from git URL`

```
https://github.com/M-A-L-bl-LLl/Mesh2PNG.git
```

**Вручную**

Скопировать папку пакета в `Packages/` проекта и добавить строку в `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.mesh2png": "file:com.mesh2png"
  }
}
```

### 🚀 Как пользоваться

#### 1. Открыть окно

`Tools → Mesh2PNG`

Окно разделено на две части: слева — список объектов и иерархия, справа — превью и настройки.

---

#### 2. Добавить объекты

Нажать **+ Add Object** — появится новая строка. Перетащить в неё **GameObject** или **Prefab** из Project или Hierarchy.

Можно добавить сколько угодно объектов. Каждый хранит свои настройки камеры и освещения независимо. Переключаться между объектами можно кликом по строке в списке или стрелками `‹` `›` в верхней панели.

Кнопка **✕** удаляет объект из списка.

---

#### 3. Настроить камеру

В превью:
- 🖱️ **Перетащить** — вращение вокруг объекта
- 🖱️ **Колесо мыши** — приближение и отдаление

В секции **Camera** (появляется когда объектов больше одного):
- **Rotation X / Y** — точные значения угла
- **Distance** — расстояние от камеры до объекта
- **Reset Camera** — сбросить к авто-подбору дистанции

---

#### 4. Работа с иерархией

Если у объекта есть дочерние объекты, слева появится панель **Hierarchy**.

- ☑️ **Чекбокс** рядом с именем — показать / скрыть дочерний объект в рендере
- 🖱️ **Клик по строке** — выбрать узел; камера автоматически подстроится под его размер, подсветится жёлтым боксом
- Повторный клик по выбранному узлу — снять выделение, камера вернётся к исходной дистанции

Выбранный узел и позиция камеры сохраняются для каждого объекта отдельно.

---

#### 5. Настроить освещение

Секция **Lighting**:

- **Ambient** — цвет фонового освещения
- **Light 1 / Light 2** — два направленных источника света
  - **Enabled** — включить / выключить
  - **Color** — цвет света
  - **Intensity** — яркость (0–5)
  - **Rotation X / Y** — направление источника

---

#### 6. Выбрать папку и сохранить

В секции **Output**:
- **Width / Height** — разрешение выходного PNG
- **Folder** — папка сохранения, кнопка **…** открывает диалог выбора

Кнопки внизу окна:
- **Capture** — сохранить текущий объект
- **Capture All** — сохранить все объекты из списка

Файл называется по имени GameObject и сохраняется в выбранную папку.

### ⚙️ Требования

- Unity **2021.3** или новее
- **Universal Render Pipeline (URP)** или Built-in RP

---

## 🇬🇧 English

### About

**Mesh2PNG** is an editor tool for creating sprites and icons from 3D objects. It works as a regular editor window — no Play Mode, no scene setup. Add an object, frame the shot, and export a PNG with a transparent background.

Transparency is computed properly: the object is rendered twice — on a black background and a white one — and the alpha channel is reconstructed from the difference. This correctly handles semi-transparent parts like glass, particles, and thin edges.

### ✨ Features

- 🎨 **Transparent background** without artifacts
- 📦 **Multiple objects** in one list, each with its own camera and lighting settings
- 🎥 **Live preview** — drag to rotate, scroll to zoom
- 🌲 **Hierarchy panel** — toggle child visibility and focus the camera on any node
- 💡 **Two directional lights** with color, intensity, and rotation controls
- 📐 **Arbitrary output resolution**

### 📦 Installation

**Via Git URL**

In Unity: `Window → Package Manager → + → Add package from git URL`

```
https://github.com/M-A-L-bl-LLl/Mesh2PNG.git
```

**Manual**

Copy the package folder into your project's `Packages/` directory and add an entry to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.mesh2png": "file:com.mesh2png"
  }
}
```

### 🚀 How to use

#### 1. Open the window

`Tools → Mesh2PNG`

The window is split into two panels: objects and hierarchy on the left, preview and settings on the right.

---

#### 2. Add objects

Click **+ Add Object** — a new row appears. Drag a **GameObject** or **Prefab** from the Project or Hierarchy window into the field.

You can add as many objects as you need. Each one stores its own camera and lighting settings independently. Switch between objects by clicking a row in the list or using the `‹` `›` arrows in the top bar.

The **✕** button removes an object from the list.

---

#### 3. Adjust the camera

In the preview:
- 🖱️ **Drag** — orbit around the object
- 🖱️ **Scroll wheel** — zoom in and out

In the **Camera** section (visible when there is more than one object):
- **Rotation X / Y** — precise angle values
- **Distance** — distance from the camera to the object
- **Reset Camera** — reset to auto-fitted distance

---

#### 4. Working with the hierarchy

If the object has children, a **Hierarchy** panel appears on the left.

- ☑️ **Checkbox** next to a name — show / hide that child in the render
- 🖱️ **Click a row** — select a node; the camera auto-fits to it and a yellow bounding box is drawn around it
- Click the selected row again — deselect, camera returns to the previous distance

The selected node and camera position are stored per object independently.

---

#### 5. Adjust lighting

In the **Lighting** section:

- **Ambient** — ambient light color
- **Light 1 / Light 2** — two directional lights
  - **Enabled** — on / off
  - **Color** — light color
  - **Intensity** — brightness (0–5)
  - **Rotation X / Y** — light direction

---

#### 6. Set output and capture

In the **Output** section:
- **Width / Height** — output PNG resolution
- **Folder** — save path, the **…** button opens a folder picker

Buttons at the bottom:
- **Capture** — save the current object
- **Capture All** — save every object in the list

Each file is named after the GameObject and saved to the selected folder.

### ⚙️ Requirements

- Unity **2021.3** or later
- **Universal Render Pipeline (URP)** or Built-in RP
