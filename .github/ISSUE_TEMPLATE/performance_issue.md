---
name: ⚡ Performance Issue
about: Report a performance problem, slowness, or regression
title: "[PERF] "
labels: performance, needs-triage
assignees: ''
---

## ⚡ Performance Issue Description
<!-- Describe the performance problem clearly. -->

## 📉 Impact
<!-- How severe is this? Does it block usage, degrade UX, or cause timeouts? -->

## 🔁 Steps to Reproduce
1. ...
2. ...
3. Observe slow/degraded behavior

## 📊 Measurements
<!-- Provide before/after metrics if possible. -->

| Metric         | Expected  | Actual    |
|----------------|-----------|-----------|
| Response time  | < 200ms   | ~2000ms   |
| Memory usage   | < 100MB   | ~800MB    |
| CPU usage      | < 10%     | ~90%      |

## 🌍 Environment
| Field        | Value           |
|--------------|-----------------|
| OS           | e.g. Ubuntu 22  |
| Version      | e.g. 2.1.0      |
| Dataset size | e.g. 50k rows   |
| Hardware     | e.g. 8GB RAM    |

## 🔬 Profiling Data
<!-- Attach profiling results, flame graphs, or traces if available. -->
<details>
<summary>Profile Output</summary>

```
Paste profiling data here
```
</details>

## 📎 Additional Context
<!-- Any other relevant information. -->