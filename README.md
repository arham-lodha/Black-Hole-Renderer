# Black Hole Renderer

By Arham Lodha and Prajith Velicheti

Extra Credit: Both of us completed the Course Evaluation Survey.

Our project was made using Unity. Our submission consists of:
- Source code in `Black-Hole-Renderer.zip`
- A video to demo our project in `Demo.mp4`
- Our report in `Report.pdf`

A detailed description of our implementation is given in the second page of the report. Here we outline the sources we used.

Credits:

We referenced the following papers, websites, and videos:
- https://www.scitepress.org/PublishedPapers/2021/102176/102176.pdf (for ray marching)
- https://blog.seanholloway.com/2022/03/13/visualizing-black-holes-with-general-relativistic-ray-tracing/ (for suggestions on how to decorate the accretion disk)
- https://www.youtube.com/watch?v=Qz0KTGYJtUk (for interactions between rays and emissive objects)
- https://www.youtube.com/watch?v=zweKSSuDEi0 (for the formula for the temperature of the accretion disk)

For the SDFs of the sphere and cube at `Black-Hole-Renderer/Assets/Shaders/sdfs.cginc`, we referenced the implementations at https://iquilezles.org/articles/distfunctions/. We used the `qtrans` method from our Mannequin project.

For the permuted congruential generator used to generate a random number in the shader, we used the implementation in https://www.youtube.com/watch?v=Qz0KTGYJtUk, which is located in the `NextRandom` and `RandomValue` functions at `Black-Hole-Renderer/Assets/Shaders/noise.cginc`. For noise based on fractional Brownian motion, we referenced the implementation at https://iquilezles.org/articles/fbm/. We used the Perlin noise generation implementation from our Minecraft project.

We referenced source code from https://github.com/HollowaySean/BlackHoleViz_v2, the repository of https://blog.seanholloway.com/2022/03/13/, to understand the algorithm behind coloring the accretion disk and accretion disk intersection.

To generate the texture used to color the accretion disk based on temperature, we generated the texture from https://blog.seanholloway.com/2022/03/13/ by porting their MATLAB script into Python, which is located at `Black-Hole-Renderer/Assets/Script/generateBlackbodyRadiation.ipynb`.

