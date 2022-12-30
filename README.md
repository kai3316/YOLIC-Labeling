# Image Annotation Tool Based on Cell-wise Segmentation
## Welcome to the YOLIC project! a novel object detection method!

In this project, we present a real-time object detection method called You Only Look at Interested Cells (YOLIC) that is based on cell-wise segmentation. Traditional object detection methods often require expensive computing resources and may not be suitable for low-cost devices. YOLIC addresses this issue by focusing on predefined interested cells (i.e., subregions) and using a single deep learner to classify all interested cells at once. This approach applies the concept of multi-label classification to object detection, and can be implemented using existing classification models.

One unique feature of this method is that the size of the cells used to represent objects can be flexibly controlled based on the specific needs of the task. This allows you to customize the level of detail and granularity of the object representation to suit your needs, rather than relying on the network to learn the optimal cell size through regression. If you have prior knowledge about the approximate range, shape, or size of the objects you want to detect, you can use a few large cells to represent them.

We have tested YOLIC using on-road risk detection, and found that it is significantly faster and more accurate than traditional object detection methods.

To use this project, you will need to install the YOLIC tool on a Windows system and design your own cell configuration (i.e., interested cells) based on actual needs. You will also need to annotate your own data using the provided annotation tool. For more information on how to use these tools, please refer to the project's documentation. 

We hope you find this project helpful, and we welcome any feedback or contributions. Thank you for your interest in YOLIC!

## Annotation Example
### Cell Configuration for a road risk detection task
![image](https://github.com/kai3316/YOLIC-Labeling/blob/master/cellExample.png)
### Annotation Tool with this configuration (Configuration3.json)
![image](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)



