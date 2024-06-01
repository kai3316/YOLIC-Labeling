# YOLIC Labeling Tool
The YOLIC Labeling Tool is a user-friendly software designed to support the You-Only-Look-at-Interested-Cells (YOLIC) methodology for real-time object detection based on cell-wise segmentation. YOLIC focuses on predefined interested cells to classify objects using a single network, addressing the issue of expensive computing resources required by traditional object detection methods.

## Key Features
- Flexible cell configuration system allowing users to import different detection areas based on task-specific requirements
- Polygon-based annotation for precise object localization
- Semi-automatic labeling with deep learning models to streamline the annotation process
- Support for both RGB and RGB-D images
- Customizable level of detail and granularity of object representation

## Getting Started
1. Install the YOLIC Labeling Tool on a Windows system
2. Design your own cell configuration (i.e., interested cells) based on actual needs using the [Cell Designer Tool](https://github.com/kai3316/Cell-designer)
3. Annotate your data using the YOLIC Labeling Tool
4. Refer to the project's code to train a YOLIC model

## Annotation Example
### Cell Configuration for a Road Risk Detection Task
![Cell Configuration Example](https://github.com/kai3316/YOLIC-Labeling/blob/master/cellExample.png)

### Annotation Tool with Configuration3.json
![Annotation Tool GUI](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)

## Contributions and Feedback
Our team is dedicated to constantly improving and expanding the capabilities of this tool. We welcome any feedback or contributions to help enhance the YOLIC Labeling Tool.

Thank you for your interest in YOLIC! We hope you find this project helpful in your object detection tasks.
