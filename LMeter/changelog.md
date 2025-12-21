# Version 0.4.3.2
- Remove leftover data sort options.

# Version 0.4.3.1
- Updated for Dalamud Api 14

# Version 0.4.2.2
- Fix problem with exporting and importing profiles

# Version 0.4.2.1
- Misc bug fixes

# Version 0.4.2.0
- Fixed calculation for Crit Direct Hit percentage
- Added option to add rounded corners to certain elements

# Version 0.4.1.5
- Updated for Dalamud Api 12
- Added decimal text tag support for Crit and Direct Hit percent

# Version 0.4.1.3
- FFLogs DPS calculation has been disabled (hopefully temporarily)
- Added feature for custom color for your own bar
- Fix 'Hide Tag Values if Zero' option not working for some tag values

# Version 0.4.1.2
- Update FFLogs integration

# Version 0.4.1.1
- Fix bug with [dps] tag not working for some users

# Version 0.4.1.0
- Add support for FFLogs DPS calculations
    - Five new TextTags added: RDPS, ADPS, NDPS, CDPS, RAWDPS
    - RAWDPS is FFLogs DPS without any calculations applied
    - Added option to sort meter by these numbers (though it is not recommended)
- Added new text option to hide text tag value if value is 0

# Version 0.4.0.2
- Fix issue with maxhitname text tag
- Added bar color selector for Limit Break

# Version 0.4.0.1
- Fix bug causing many numbers to show 0

# Version 0.4.0.0
- Redesigned Text options to allow significantly more customization
    - Can now create as many separate bar texts as you would like
    - Bar Texts can now be configured with fixed width
    - Bar Texts can be anchored relative to other texts
    - The above features allow creation of data columns
    - Added option for a column header bar
    - And a lot more...
- Added option for a Footer bar
- Overhauled Visibility settings (again)
- Bar height can now be set to a specific value
- Added option for thinner bars (similar to Kagerou)
- Added option for job icon background color
- OTF fonts can now be added to the font list

# Version 0.3.1.0
- Updated plugin for Dawntrail
- Added support for Viper and Pictomancer
- Window clipping logic was improved
- Improved the Visibility options for more complex configurations

# Version 0.3.0.1
- Fix bug with fonts not copying to config directory
- Fix bug with ACT config not loading
- Fix issue with changelog not loading

# Version 0.3.0.0
- Merged downstream changes from Tischel's repository
- Added IINACT IPC support
- Added Clipping functionality for meter to not draw over game UI elements
- Various code cleanup and bug fixes

# Version 0.1.5.3
- Fix bug that that caused removal of custom added fonts.

# Version 0.1.5.2
- Added new text tags: effectivehealing, overheal, overhealpct, maxhitname, maxhitvalue
- Bars are now sorted by effective healing when the Healing sort mode is selected.
- Added option to use Job color for bar text color
- Fixed an issue with fonts on first time plugin load

# Version 0.1.5.1
- Fixed issue with auto-reconnect not working
- Fixed issue with name text tags
- Fixed issue with borders when Header is disabled
- Fixed issue with 'Return to Current Data' option
- Added new toggle option (/lm toggle <number> [on|off])

# Version 0.1.5.0
- Added Encounter history right-click context menu
- Added Rank text tag and Rank Text option under bar settings
- Fix problem with name text tags when using your name instead of YOU

# Version 0.1.4.3
- Fix potential crash with certain text tags
- Add position offsets for bar text
- Add option for borders only around bars (not header)

# Version 0.1.4.2
- Fix issue with ACT data not appearing in certain dungeons
- Improve logic for splitting encounters

# Version 0.1.4.1
- Fix potential plugin crash
- Fix bug with lock/click through
- Disable preview when config window is closed
- Force show meter when previewing

# Version 0.1.4.0
- Added advanced text-tag formatting (kilo-format and decimal-format)
- Text Format fields have been reset to default (please check out the new text tags!)
- Added text command to show/hide Meters (/lm toggle <number>)
- Added text command to toggle click-though for Meters (/lm ct <number>)
- Added option to hide Meter if ACT is not connected
- Added option to automatically attempt to reconnect to ACT
- Added option to add gaps between bars
- Added "Combat" job group to Visibility settings
- Fixed various bugs and improved performance

# Version 0.1.3.1
- Make auto-end disabled by default

# Version 0.1.3.0
- Add options to end ACT encounter when combat ends

# Version 0.1.2.0
- Update for Endwalker/Dalamud api5
- Add Reaper/Sage support
- Add Scrolling

# Version 0.1.1.0
- Fix sorting
- Fix bug with texture loading
- Fix default websocket address

# Version 0.1.0.0
- Created Plugin
