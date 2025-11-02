#!/bin/bash
# Script to capture CameraSphereController debug logs from Quest 3

echo "=== Connecting to Quest 3 and capturing logs ==="
echo "Make sure your Quest 3 is connected via USB and USB debugging is enabled"
echo ""
echo "Filtering for CameraSphereController logs..."
echo "Move your head in VR, then press Ctrl+C to stop capturing"
echo ""

# Clear old logs first
adb logcat -c

# Capture and filter logs
adb logcat | grep -i "CameraSphereController"

