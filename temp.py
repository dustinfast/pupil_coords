#!/usr/bin/env python

# List Reg keys
import _winreg as reg
from itertools import count

key = reg.OpenKey(reg.HKEY_LOCAL_MACHINE, 'HARDWARE\\DEVICEMAP\\VIDEO')
try:
    for i in count():
        device, port = reg.EnumValue(key, i)[:2]
        print(device, port)
except WindowsError:
    pass

# For WMI Code Creator
from infi.devicemanager import DeviceManager
dm = DeviceManager()
dm.root.rescan()
disks = dm.disk_drives
names = [disk.friendly_name for disk in disks]