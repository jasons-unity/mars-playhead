# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.1] - 2019-11-25
### Fixed
- Fixed issue where the handles would also draw in the game view
- Fixed invalid handle size in builds
- Fixed drawing of UGUI Canvas in handles 

## [0.4.0] - 2019-10-17
### Added
- Optimization: Stop rendering the handles of a context if none of them are active
- Added support for URP and HDRP
- Added Editor Handle State Colors as a way to change the color of a renderer based on the state of the handle using the color preferences
- Added an event when a handle is created by a Handle Context

### Changed
- Merged Hover Color and Drag Color into Handle State Colors

### Removed
- Removed unimplemented default handles as to not cause confusion

### Fixed
- Fixed issue with picking being offset in the scene view
- Fixed possible exception in the position handle
- Fixed handle rendered with a frame of delay after a mouse move
- Fixed issue with rendering on non 1x screens
- Fixed errors when enabling/disabling a HandleBehaviour during an event
- Fixed rendering issue when using 2020.1
- Fixed deactivated child behaviours getting events after handle is destroyed

### Known Issues
- Picking is higher on higher resolution screens

## [0.3.0] - 2019-09-03
### Added
- Added a ScaleWithCameraDistance component

### Changed
- Events are now sent to all component inheriting HandleBehaviour instead of interface classes
- Moved all components inside the PrefabHandles AddComponentMenu
- Made HandleUtility.GetHandleSize public
- Moved the handle picking to a separate assembly

### Removed
- Removed IHoverListener, IDragListener and IHandleRenderingListener in favor of HandleBehaviour

### Fixed
- Fixed handles when in orthographic camera mode
- Fixed issue where IMGUI handles and prefab handles could be hovered at the same time in the scene view
- Fixed slider1D gizmo being in the wrong place
- Fixed picking in non 1x resolution
- Fixed issue where EditorWindowContext would repaint active scene view and not it's window
- Fixed initial local position of SliderHandleBase

### Known Issues
- Wrong handle scale on non editor devices
- Slight delay between the scene view render and the handle render
- Picking is offset in scene view by about 10 pixels in the Y axis

## [0.2.0] - 2019-05-01
### Added
- Added code documentation of picking
- Added tests for default PickingTargets and ScreenPickingUtility
- Added tests for context creation/destruction

### Changed
- Changed context to use GameObjects instead of HandleInfo
- Changed Pickers are now PickingTargets
- Changed PickingTargets to now return a mesh instead of having a GetDistanceToMouse function

### Known Issues
- Scale Tool doesn't work properly when zoomed out

## [0.1.0] - 2019-03-12
### Added
- Created Package with handle system prototype

### Known Issues
- Scale Tool doesn't work properly when zoomed out
