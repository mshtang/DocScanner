# DocScanner
DocScanner detects a "page"-like document in a picture taken by a mobile phone, tries to move the shadow cast by the photo-taker, and adjusts the image to compensate the distortion. Like any other similar apps, the detection of the page doesn't work perfectly in any circumstance due to the variety of the background, lighting and the shadow on the pager etc. Thus, manaul adjustment, which is also provided by the app, is necessary in such case.

**This project demonstrates the development of a WPF app (implemented by MVVM design pattern) with image processing using OpenCVSharp as backbone.**

# Screentshots
## Successful detection
Full screen:

![test1](https://user-images.githubusercontent.com/32868278/107338957-6a9cf800-6abc-11eb-80c9-c9e988982ed4.jpg)
![test2](https://user-images.githubusercontent.com/32868278/107338993-74266000-6abc-11eb-9cd9-64f9c3ce7758.jpg)

Small window:

![test3](https://user-images.githubusercontent.com/32868278/107339083-8c967a80-6abc-11eb-9447-5f94f08cab46.jpg)
![test4](https://user-images.githubusercontent.com/32868278/107339087-8e603e00-6abc-11eb-81a6-0aa4e98cca0e.jpg)

The app handles the rescaling the bounding box upon window being resized automatically. In any window size, a processed image with full resolution can be saved.

## Failed detection but can be corrected manually
![fail](https://user-images.githubusercontent.com/32868278/107339004-78eb1400-6abc-11eb-8cce-9d2c3d964777.jpg)
Adjust by dragging the corner points:
![adjust](https://user-images.githubusercontent.com/32868278/107339025-7d173180-6abc-11eb-9f64-b02e2ea4d315.jpg)
![result](https://user-images.githubusercontent.com/32868278/107339069-886a5d00-6abc-11eb-9b27-e926cd7663d5.jpg)

