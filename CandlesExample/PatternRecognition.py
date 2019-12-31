import numpy
import numpy as np
from talib._ta_lib import CDL3INSIDE
import talib._ta_lib as ta
types = ['f8', 'f8', 'U50', 'i4', 'i4', 'i4', 'i4', 'i4']
data_path = "data/nyc_bike_racks.csv"
data = np.genfromtxt(data_path, dtype=types, delimiter=',', names=True)

ta.CDL2CROWS()
CDL3LINESTRIKE()
integer = CDL3INSIDE(data[], high, low, close)
numpy.savetxt("d:\\sma.csv", integer, delimiter=",")

