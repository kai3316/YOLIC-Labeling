# Image Annotation Tool Based on Cell-wise Segmentation
## Welcome to a novel object detection method!

This method can be seen as a combination of object detection and semantic segmentation. While object detection uses rectangular bounding boxes to represent the position of an object and semantic segmentation separates the object from the background at the pixel level, cell-wise segmentation uses several cells to represent a single object.

One unique feature of this method is that the size of the cells used to represent objects can be flexibly controlled based on the specific needs of the task. This allows you to customize the level of detail and granularity of the object representation to suit your needs, rather than relying on the network to learn the optimal cell size through regression. If you have prior knowledge about the approximate range, shape, or size of the objects you want to detect, you can use a few large cells to represent them.

One advantage of this approach is that it can more accurately depict the location of an object by showing each component of the target object. This is especially useful for objects with uncertain shapes or occluded components.

In addition, the cell-wise segmentation method is more efficient at segmenting high-resolution images compared to semantic segmentation, and can significantly reduce the complexity of segmentation-based detection methods.

Overall, the cell-wise segmentation method is a useful tool for accurately and efficiently detecting and segmenting objects in images. We hope you find it helpful in your work!
## Annotation Tool
![image](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)

Annotation Tool with Configuration3(Configuration3.json)

