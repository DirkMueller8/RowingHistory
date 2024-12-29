## Analysis of Rowing History with Concept II
**********************************************
Software:	&emsp;	C# 10

Version: &emsp;   	1.0

Date: 	&emsp;		Dec, 28, 2024

Author:	&emsp;		Dirk Mueller
**********************************************
This is software for reading raw rowing history from my logbook of sessions on the Concept II rowing machine, spanning 1993 to 2024.  
The entries were orginally in units of pace, that is how long - on average - was needed to cover the distance or duration, in the format

    01.03.1993,2:03.0,30 min
    17.06.2013,2:18.0,10 min
    26.06.2013,2:04.4,1000 m
    08.01.2015,2:16.2,20 min
    12.01.2015,2:15.5,2500 m
    14.01.2015,2:12.0,2000 m
    18.01.2015,2:18.9,2500 m
    12.07.2015,2:16.0,2500 m

It uses the ScottPlot library from Nuget.

![Alt text](/RowingHistory/Images/power.png)

*Fig 1: Raw data displayed as Power vs. Date for all log entries*

For the purpose of having better comparability the software is designed to group the sessions, see Fig. 2: 
the 2500 m sessions only.

![Alt text](/RowingHistory/Images/power_no_less_than_2500m.png)

*Fig 2: Raw data displayed as Power vs. Date for all log entries where distance records over or equal to 2500 m are concerned*

To look closer at the time period after I bought my own Concept II in 2015 I narrowed down the view accordingly, see Fig. 3:  

![Alt text](/RowingHistory/Images/input_no_less_than_2500m_wo_1993.png)

*Fig 3: Raw data displayed as Power vs. Date for all log entries where distance records over or equal to 2500 m are concerned*

To get an idea on the age-related deterioration of power output i added a regression curve

![Alt text](/RowingHistory/Images/input_no_less_than_2500m_regression_early.png)

*Fig 4: Log entries where distance records over or equal to 2500 m are concerned in a sub category*

![Alt text](/RowingHistory/Images/input_no_less_than_2500m_regression_late.png)

*Fig 5: Log entries where distance records over or equal to 2500 m are concerned in a sub category*