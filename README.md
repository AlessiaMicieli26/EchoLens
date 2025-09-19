# EchoLens: Augmented Reality That Gives Voice to Space

An application for Oculus Meta Quest 3 to support visually impaired users
Developed at the University of Catania – Master’s Degree in Computer Science

📌 Overview

EchoLens is a Mixed Reality application designed for the Oculus Meta Quest 3.
Its goal is to provide real-time environmental awareness for visually impaired people by transforming video streams into audio descriptions.

By combining computer vision, artificial intelligence, and wearable immersive technology, EchoLens allows users to "hear" their surroundings, enhancing independence and accessibility.

✨ Features

Real-time object detection using YOLOv8

Video streaming from Meta Quest 3 passthrough cameras to a Python server

Automatic server discovery via UDP broadcast

WebSocket-based low-latency streaming

Text-to-Speech feedback for immediate and accessible descriptions

Spatial Anchors: allows users to create and maintain persistent virtual objects using natural pinch gestures

Hand tracking for intuitive interaction (pinch-based anchoring)

🛠️ Architecture

The system is composed of two main components:

Unity Client (Oculus Quest 3)

Captures passthrough frames

Handles hand tracking and pinch gestures

Manages UI and TTS playback

Streams data via WebSocket

Python Server

Receives and processes video frames

Applies YOLOv8 object detection

Generates scene analysis (objects, brightness, contrast, clarity)

Provides textual and vocal feedback asynchronously

🎮 Unity Project Structure

Camera Rig → manages immersive perspective tracking

Passthrough → displays real-world environment with digital overlays

Real Hands → supports hand tracking & gestures

Text-to-Speech (TTS) → converts recognition results into spoken output

Streaming Module → captures frames and streams them to the server

Spatial Anchor Core → allows placement and persistence of virtual objects

Controller Buttons Mapper → alternative interaction via controllers

🚀 How It Works

Server Discovery

Unity client broadcasts request → Python server replies with IP

Video Streaming

Frames are captured, compressed, and sent via WebSocket

Frame Processing

Server selects key frames (from batches of 8)

YOLOv8 detects objects and evaluates scene conditions

Generates text + audio description

Feedback

Unity client displays text and plays speech in real-time

🧑‍🤝‍🧑 Use Case

Designed for visually impaired users

Provides real-time environmental descriptions

Gesture-based interaction (pinch) for creating spatial anchors

Enhances independence through immersive assistive technology

📈 Future Improvements

Full voice-guided navigation and menu interaction

Extended accessibility features (e.g., customizable audio feedback)

Integration with more advanced multimodal AI models

📚 References

OrCam MyEye

Envision Glasses

Aira

Be My Eyes

Comparative study on MR pass-through quality

👩‍💻 Author

Alessia Micieli
Supervisors: Prof. Giovanni Maria Farinella, Dr. Michele Mazzammuto
University of Catania – Academic Year 2024/2025
