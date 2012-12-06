@echo off
set round = 0

:epic

set /a round = round + 1
echo Game %round% (Tijn)
tcpclient\tcpclient b2ki.science.uu.nl 2081 CherryMillAnt\bin\Release\CherryMillAnt.exe 3855473 CherryMillAnt
cls

set /a round = round + 1
echo Game %round% (Jordi)
tcpclient\tcpclient b2ki.science.uu.nl 2081 CherryMillAnt\bin\Release\CherryMillAnt.exe 3835634 CherryMillAnt
cls

goto epic