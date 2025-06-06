# YOLIC Labeling Tool
The YOLIC Labeling Tool is a user-friendly software designed to support the You-Only-Look-at-Interested-Cells (YOLIC) methodology for real-time detection based on cell-wise segmentation. YOLIC focuses on predefined interested cells to classify objects using a single network, addressing the issue of expensive computing resources required by traditional object detection methods.

Please note that this tool is primarily designed for annotating images to train YOLIC models, rather than directly recognizing objects in images. The YOLIC Labeling Tool enables users to define regions of interest and label objects within those regions, creating annotated datasets that can be used to train YOLIC models for efficient object detection.

## Key Features
- Flexible cell configuration system allowing users to import different detection areas based on task-specific requirements
- Polygon-based annotation for precise object localization
- Semi-automatic labeling with deep learning models to streamline the annotation process
- Support for both RGB and RGB-D images
- Customizable level of detail and granularity of object representation

## Getting Started

  To install the software, please follow these steps:
  1. Go to the "Releases" section of the repository.
  2. Download the `Tool.msi` file, which is a pre-compiled installer.
  3. Run the `Tool.msi` file on your Windows machine to install this annotation tool.
  4. For detailed information on using the software and its various features, please refer to the `Usage.pdf` file in the repository.
     
To test the annotation tool, you can use the images provided in the `test image.zip` file along with the `Configuration3.json` configuration file to get started.

## Started with your own data

  1. Install the YOLIC Labeling Tool.
  2. Design your own cell configuration (i.e., interested cells) based on actual needs (locations and shapes) using the [Cell Designer Tool](https://github.com/kai3316/Cell-designer), or manually input the label information and COIs coordinate information according to the JSON format. COIs currently support rectangles and polygons. Rectangles require the coordinates of the top-left and bottom-right points, while polygons require the coordinates of each point in order.

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
 ```
  3. Annotate your data using the YOLIC Labeling Tool.
  4. Refer to the project's [code](https://github.com/kai3316/YOLIC_code) to train a YOLIC detection model.
     
For more information on the YOLIC method, please refer to the paper ["YOLIC: An Efficient Method for Object Localization and Classification on Edge Devices" ](https://arxiv.org/abs/2307.06689).

## Annotation Example

### Cell Configuration for a Road Risk Detection Task
![Cell Configuration Example](https://github.com/kai3316/YOLIC-Labeling/blob/master/cellExample.png)
### Annotation Tool with Configuration3.json
![Annotation Tool GUI](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)

## Contributions and Feedback
Our team is dedicated to constantly improving and expanding the capabilities of this tool. We welcome any feedback or contributions to help enhance the YOLIC Labeling Tool.

Thank you for your interest in YOLIC! We hope you find this project helpful in your detection tasks.
