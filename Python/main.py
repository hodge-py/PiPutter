import os
from ultralytics import YOLO
from pathlib import Path


project_dir = os.getcwd()
dataset = Path(__file__).parent.parent / 'yolo'

YamlFile = Path(__file__).parent.parent / 'yolo' / 'data.yaml'
print(YamlFile)


model = YOLO('yolov8n.pt')

model.train(data=YamlFile, epochs=30, imgsz=640, project=project_dir, name='yolov8n_custom', exist_ok=True)
model.val()

model = YOLO(Path(__file__).parent.parent / 'yolov8n.pt')
model.export(format='onnx')