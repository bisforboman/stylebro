### Fixed

- **bro check** right after a clean `bro format` answers from that format's check again on .NET SDK 10.0.1xx in
  multi-targeted repositories, instead of running every check a second time: the note that IDE0049's fixes are held
  back on that SDK no longer turns the shortcut off (it is still printed).
