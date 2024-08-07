# Image Annotation Tool Based on Cell-wise Segmentation
## Cell-wise segmentation
Cell-wise  segmentation  is  a  relatively  new  approach  forannotating  objects  in  images.  The  method  can  be  viewed  as a  task  between  object  detection  and  semantic  segmentation. Cell-wise  segmentation  does  not  use  a  rectangular  bounding box to represent the object’s position, nor does it separate the object  from  the  background  at  the  pixel  level  like  semantic segmentation.  Instead,  it  uses  several  cells  to  rep-resent  one  object.  Compared  with  object  detection  methods, cell-wise  segmentation  method  can  depict  each  componentof  the  target  object,  thereby  more  accurately  representingthe  object’s  location.  In  this  way,  the  cell-wise  segmentationmethod  is  more  suitable  for  objects  with  uncertain  shapesor  objects  with  occluded  components.  Our  method  can  also more  efficiently  segments  high-resolution  images  comparedwith  semantic  segmentation,  and  can  significantly  reduce  thecomplexity  of  segmentation-based  detection  methods. we can flexibly control the size of the cell according to our actual needs instead of training network to learn the size  of  cell  through  regression.  If  we  have  prior  knowledge about the approximate range, shape or size of the object, we can use a few large cells to depict the objects.
## Annotation Tool
![image](https://github.com/Inceptionnet/YOLIC-Labeling/blob/master/images/LabelingGUI.png)

Annotation Tool with Configuration3(Configuration3.json)

