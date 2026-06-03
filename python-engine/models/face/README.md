# 얼굴 모델

필수 파일:

```text
yolo26x_rafdb_best.pt
```

모델 정보:

```text
사용 데이터: RAF-DB
데이터셋 신청: http://www.whdeng.cn/RAF/model1.html
학습 방식: Ultralytics YOLO 분류 모델 학습
출력: 얼굴 표정 class별 확률
```

SHA-256:

```text
48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a
```

사용 코드:

```text
python-engine/src/mouth_of_truth/face/infer_face.py
python-engine/src/mouth_of_truth/face/face_score_logic.py
```

모델을 교체할 때는 Ultralytics YOLO 분류 체크포인트를 같은 파일명으로 배치하고, `face_score_logic.py`가 기대하는 레이블과 점수 매핑을 맞춥니다.
