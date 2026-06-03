# Face Model Assets

Required local file:

```text
yolo26x_rafdb_best.pt
```

Lineage:

- dataset: RAF-DB
- model family: Ultralytics YOLO classification
- runtime loader: `python-engine/src/mouth_of_truth/face/infer_face.py`
- score rules: `python-engine/src/mouth_of_truth/face/face_score_logic.py`

The trained `.pt` file is not tracked in Git. Copy it into this directory before
running the Python bridge with real face analysis.
