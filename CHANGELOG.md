# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
### Changed 
### Deprecated
### Fixed

##[0.8.0] - 2019-07-27  
This is the first version compatible with UCR 0.8.x  
### Added
- Added DeltaToButtons plugin
- Added validation where appropriate
### Changed 
### Deprecated
### Removed
### Fixed
- Fixed Reset for DeltaToAxis Relative mode
- AxesToAxesRotation no longer wraps

##[0.0.4] - 2019-01-03
### Changed
- Update plugins to new UCR 0.7.0 format

## [0.0.3] - 2018-10-29
### Changed 
- Renamed AxisToAxisCumulative to AxisToAxisIncrement
- Included a counter effect to AxisToAxisIncrement
### Fixed
- Each instance of ButtonToButtonsLongPress uses their own/private longPressTimer 
- Each instance of AxisToDelta needs uses own/private timer 
- Each instance of DeltaToAxis needs uses own/private timer 


## [0.0.2] - 2018-10-29
### Added
- Added EventToButton plugin

## [0.0.1] - 2018-10-02
### Added
- Moved AxisToAxisCumulative, AxisToDelta, ButtonToButtonsLongPress and DeltaToAxis plugins from UCR repo to this repo
