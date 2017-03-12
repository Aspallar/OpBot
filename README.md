# OpBot

All commands must start with a mention to OpBot (e.g. @OpBot).

Parameters enclosed in square brackets [] are optional.

## Create a new operation event
@OpBot create \<op> \<size> \<day> [\<time>] [\<mode>]

\<op> is one of  
EV  Eternity Vault  
KP  Karagga's Palace  
EC  Explosive Conflict  
TFB Terror From Beyond  
SV  Scum and Villainy  
DF  The Dread Fortress  
DP  The Dread Palace  
RAV The Ravagers  
TOS Temple of Sacrifice  
GF  Group Finder  

\<size> is 8 or 16

\<day> is the day of the operation (mon, tue, wed etc)

\<time> is the time of operation in UTC, defaults to 19:30 if omitted.

\<mode> is one of (defaults to SM if omitted)  
SM  
VM  
MM  

#### Examples:
@OpBot create KP 8 Fri  
@OpNot create rav 16 Thu 16:00 VM

## Signup yourself
@OpBot \<primary role>

\<primary role> is one of  
tank  
dps  
heal

#### Examples
@OpBot dps  
@OpBot tank 

## Sign someone else up
@OpBot @\<user to signup> \<primary role>

#### Example
@OpBot @Aspallar heal

## Remove a signup (for yourself)
@OpBot remove

## Remove a signup (for someone else)
@OpBot remove @\<user to remove>

#### Example

@OpBot @Aspallar remove

## Changing a role

To change your role just signup again with the new role.

## Add a note
@OpBot addnote \<The text of the note>

## Features to be added in a later version

* Allow alternate roles to be specified.  
* Allow editing of operation details (changing the time, mode etc)  
* Allow specifying a date more than 7 days ahead. 
* Other stuff \:P
 
