import numpy as np

# display matrix non transposed::
#0.00000	-0.82051	0.00000	0.00000
# -1.00000	0.00000	0.00000	0.00000
# 1.00000	0.91026	1.00000	0.00000
# 0.00000	0.00000	0.00000	1.00000
M = np.array([[0.00000, -0.82051, 0.00000, 0.00000],
              [-1.00000, 0.00000, 0.00000, 0.00000],
              [1.00000, 0.91026, 1.00000, 0.00000],
              [0.00000, 0.00000, 0.00000, 1.00000]])

uvs = [np.array([0,0,1,0]),
       np.array([1,0,1,0]),
       np.array([1,1,1,0]),
       np.array([0,1,1,0])]


mapped = [M.T @ uv for uv in uvs]

topCrop = 1.0-0.91026

scaleY = 1.0 / (1.0-2.0*topCrop)

for map in mapped:
    map[1] = (map[1] - topCrop) * scaleY



print("Mapped UVs:")
for uv in mapped:
    print(uv)



