![logo](src/.editoricon.png)

# Rocket Broadcast Plugin
Provides automated In-Game broadcast messages 

![build](https://img.shields.io/github/v/release/lisiados-dev/rocket-plugins-broadcast.svg)

---------------------------------------

See the [changelog](CHANGELOG.md) for changes.

## Table of contents

* [Roadmap](#roadmap)
* [Installing](#installing)
* [Configuring](#configuring)
* [Permissions](#permissions)
* [License](#license)

### Roadmap

- [x] Join Messages
- [x] Leave Messages
- [x] Death Messages
- [x] Announcements
- [x] Text Commands
- [x] GeoIP Join Messages
  - [x] IPInfo (ipinfo)
  - [x] IP API (ipapi)

### Installing

Copy binary to rocket's plugins folder and start server.

### Configuring

 - Add messages in `<Messages>` node for automatically broadcast messages on server.
 - Add custom commands in `<Commands>` node for custom text commands execution.
 - Configure var for all modules, Ej. `AnnouncementsInterval`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Config xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Messages>
    <Message Text="Type [/rules] to read the server rules" Color="green" />
  </Messages>
  <Commands>
    <Command>
      <Name>rules</Name>
      <Help>Shows the server rules</Help>
      <Text>
        <Line>#1 Kill</Line>
        <Line>#2 Survive</Line>
        <Line>#3 Build</Line>
      </Text>
    </Command>
  </Commands>
  <AnnouncementsEnable>true</AnnouncementsEnable>
  <AnnouncementsInterval>180</AnnouncementsInterval>
  <JoinMessageEnable>true</JoinMessageEnable>
  <LeaveMessageEnable>true</LeaveMessageEnable>
  <DeathMessageEnable>true</DeathMessageEnable>
  <JoinMessageColor>green</JoinMessageColor>
  <LeaveMessageColor>green</LeaveMessageColor>
  <DeathMessageColor>red</DeathMessageColor>
  <GroupMessages>false</GroupMessages>
  <ExtendedMessages>false</ExtendedMessages>
  <SuicideMessages>true</SuicideMessages>
  <ShowJoinCountry>true</ShowJoinCountry>
  <GeoIpProvider>ipinfo</GeoIpProvider>
</Config>
```

### Permissions

No permissions required for now

### License

Code released under [The MIT License](LICENSE)