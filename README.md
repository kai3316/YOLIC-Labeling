# Image Annotation Tool Based on Cell-wise Segmentation
## Welcome to the YOLIC project! a novel object detection method!

In this project, we present a real-time object detection method called You Only Look at Interested Cells (YOLIC) that is based on cell-wise segmentation. Traditional object detection methods often require expensive computing resources and may not be suitable for low-cost devices. YOLIC addresses this issue by focusing on predefined interested cells (i.e., subregions) and using a single network to classify all interested cells at once. This approach applies the concept of multi-label classification to object detection, and can be implemented using existing classification models.

One unique feature of this method is that the size of the cells used to represent objects can be flexibly controlled based on the specific needs of the task. This allows you to customize the level of detail and granularity of the object representation to suit your needs, rather than relying on the network to learn the optimal cell size through regression. If you have prior knowledge about the approximate range, shape, or size of the objects you want to detect, you can use this information to design a more effective cell configuration. For example, if you know that the objects of interest are typically small and will be located in a specific area of the image, you can design a denser cell configuration for that region. Similarly, if you know that the objects are typically large and will be located throughout the image, you may want to use a coarser cell configuration that covers a larger area. Utilizing this prior knowledge can help to ensure that the cells are able to effectively cover the objects of interest, while minimizing the number of cells needed and reducing the computational burden.

We have tested YOLIC using on-road risk detection, and found that it is significantly faster and more accurate than traditional object detection methods.

To use this project, you will need to install the YOLIC tool on a Windows system and design your own cell configuration (i.e., interested cells) based on actual needs (the cell designer tool can be found at https://github.com/kai3316/Cell-designer). You will also need to annotate your own data using this annotation tool. For more information on how to use these tools, please refer to the project's documentation. 

Our team is dedicated to constantly improving and expanding the capabilities of this tool, we hope you find this project helpful, and we welcome any feedback or contributions. Thank you for your interest in YOLIC!

## Annotation Example
### Cell Configuration for a road risk detection task
![image](https://github.com/kai3316/YOLIC-Labeling/blob/master/cellExample.png)
### Annotation Tool with this configuration (Configuration3.json)
![image](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)



