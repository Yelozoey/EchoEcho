# S1-P06-B 实际景深

复测1/1通过，B-NativeBokeh-Retest.json。真实射线目标1m→独立Profile实例→原生Bokeh，近/远图像对比与5个Bokeh事件；UI占用、遮挡和失焦后退出，源Profile焦距/光圈不变。FocusOn/Off PNG已查看：近板清晰、远板模糊。首轮图像通过但FrameDebugger后应用焦点未恢复，F067修复夹具后重跑全部，保留首失败。

渐变以状态.15秒将光圈32→5.6（渐出反向），实际Volume仅激活/关闭，避免非插值枚举开关产生假渐变；目标平滑10/s不变。并非混合两个同焦距Profile就能保证线性模糊强度。临时渲染层30隔离，URP Unlit不依赖外部灯光；未覆盖R02资产。环境/硬件同A，Windows待C实测。
