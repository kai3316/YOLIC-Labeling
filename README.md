# YOLIC Labeling Tool

The **YOLIC Labeling Tool** is a user-friendly annotation software designed to support the **You-Only-Look-at-Interested-Cells (YOLIC)** methodology for real-time object detection based on cell-wise segmentation.

Unlike traditional object detection methods that require scanning the entire image (leading to high computational cost), YOLIC focuses only on **predefined Interested Cells (COIs)**. This enables efficient detection and classification using a single lightweight network — ideal for edge devices.

> ⚠️ **Important:**
> The YOLIC Labeling Tool is designed for **annotating datasets to train YOLIC models**, not for direct object recognition.

---

## ✨ Key Features

* **Flexible cell configuration system**  
  Import custom detection areas based on task requirements.

* **Polygon-based annotation**  
  Precise object localization within interested cells.

* **Semi-automatic labeling**  
  Integrates deep learning models to accelerate annotation.

* **RGB and RGB-D support**  
  Works with both standard and depth-enhanced images.

* **Customizable granularity**  
  Control the level of detail in object representation.

---

# 🚀 Getting Started

You can install and use the YOLIC Labeling Tool in two ways:

* **Option 1:** Install directly using the precompiled `.msi` installer (recommended for most users)
* **Option 2:** Build the software from source code (recommended for developers)

---

## 🔹 Option 1: Install Using the MSI Installer (Recommended)

1. Go to the **Releases** section of the repository.
2. Download the `Tool.msi` file.
3. Run the installer on your Windows machine.
4. Follow the installation instructions.

For detailed usage instructions, please refer to the `Usage.pdf` file included in the repository.

### 🧪 Test the Tool

You can test the annotation tool using:

* `test image.zip`
* `Configuration3.json`

These files provide a ready-to-use example configuration.

---

## 🔹 Option 2: Build from Source Code

If you want to modify or develop the tool further, you can build it from source.

### Step 1: Download the Project

1. Clone or download the entire repository to your local machine.
2. After downloading, you will get a `.zip` file.
3. **Right-click the zip file → Select “Properties”.**
4. In the Properties panel, check **“Unblock”**, then click **Apply**.

> ⚠️ **Why is this necessary?**  
> Windows may block files downloaded from the internet for security reasons.  
> If you do not unblock the zip file before extracting it, the project may fail to run properly.

5. Extract the zip file.

---

### Step 2: Install Visual Studio

1. Download and install **Visual Studio**.
2. During installation, make sure to select:

   ✅ **.NET desktop development**

---

### Step 3: Open the Project

1. Open the extracted project folder.
2. Locate the file:
```

YOLIC.sln

````
3. Open it using Visual Studio.

---

### Step 4: Restore NuGet Packages

1. Right-click the **Solution** in Visual Studio.
2. Click **Restore NuGet Packages**.
3. Wait for the installation to complete.

After restoration:

* Click the **Start** button in Visual Studio to build and run the project.

---

### 🔧 If You Encounter NuGet Errors

If NuGet packages fail to install:

1. In Visual Studio’s top menu, click: Tools → Manage NuGet Packages for Solution
2. Update the required packages to the **latest compatible version**.
3. Restore packages again and rebuild the solution.

---

# 📦 Getting Started with Your Own Data

## 1️⃣ Install the Tool

Install via MSI or build from source as described above.

---

## 2️⃣ Design Your Own Cell Configuration

You can design your own Interested Cells (COIs) directly within the **YOLIC Labeling Tool** using the **Cell Designer Panel**. This feature has been integrated into the software to simplify the configuration process.

Alternatively, you can still manually edit the JSON configuration file if preferred.

### Supported COI Types

* **Rectangle**
* Requires top-left and bottom-right coordinates
* **Polygon**
* Requires ordered coordinates of all vertices

### Example Configuration

```json
{
"Labels": {
 "LabelList": [
   "Bump", "Column", "Dent", "Fence",
   "Creature", "Vehicle", "Wall", "Weed", "ZebraCrossing",
   "TrafficCone", "TrafficSign"
 ],
 "LabelAbbreviation": [
   "Bp", "Cn", "Dt", "Fe", "Ce",
   "Ve", "Wl", "Wd", "ZC", "TC", "TS"
 ],
 "LabelNumber": 11
 },
 "COIs": {
   "COINumber": 3,
   "1": ["rectangle", 0.33962, 0.34583, 0.04009, 0.07083],
   "2": ["rectangle", 0.37971, 0.34583, 0.04009, 0.07083],
   "3": ["rectangle", 0.71226, 0, 0.07075, 0.125]
 }
}
````

---

## 3️⃣ Annotate Your Data

Use the YOLIC Labeling Tool to:

* Load your images
* Import your configuration
* Label objects within defined COIs

---

## 4️⃣ Train a YOLIC Model

Refer to the training repository:

👉 [https://github.com/kai3316/YOLIC_code](https://github.com/kai3316/YOLIC_code)

---

# 🧠 Learn More About YOLIC

For detailed methodology and experimental results, please refer to the paper:

**YOLIC: An Efficient Method for Object Localization and Classification on Edge Devices**
[https://arxiv.org/abs/2307.06689](https://arxiv.org/abs/2307.06689)

---

# 📌 Annotation Example

## Cell Configuration for a Road Risk Detection Task

![Cell Configuration Example](https://github.com/kai3316/YOLIC-Labeling/blob/master/cellExample.png)

## Annotation Tool with Configuration3.json

![Annotation Tool GUI](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)

---

# 🤝 Contributions & Feedback

We are continuously improving the YOLIC Labeling Tool and welcome:

* Bug reports
* Feature requests
* Pull requests
* Suggestions for improvements

Thank you for your interest in YOLIC!
We hope this tool helps you build efficient and lightweight detection systems 🚀

