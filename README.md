# GFL_l2d_MotionConverter
 Convert extracted UABE dumps of GFL live2d motion/physics files to usable motion3/physics3 files.

 ## Liscenses
 This project is published under GPL 3.0 liscences with the following exceptions:
 1. Due to the reverse-engineering nature of this program, it can be used against the creator(Sunborn Inc. or other developer who are unwilling to find their asset converted)'s wishes, and the creators can require it to be taken down. Under such circumstances, modifying and redistribution of the code and release packages are disallowed.
 2. By using or modifying this software, you agree that you will ONLY use it for *non-commercial or private purposes*. For example, provide Live2D previewing utilities to your Fandom Wiki Page related to the game, under the condition that your wiki is non-profitable.
 3. The developer of this software will not take legal responsiblities for usages outside the given scope of the liscense and exceptions.

 ## Usage

 ### Prerequisites
 
 You will need UABE to extract textures, dumps and raw data from asset bundles.(https://github.com/DerPopo/UABE/releases)
 
 You will need a Hex-Editor(Visual Studio will suffice) to made a few modifications to get a moc3 model file.

 ### Data Extraction
 1. Find the corresponding asset bundle of your desire. A bundle containing Live2D will generally be above 10M in size. As of 2021/12/26, it's file name will contain the T-doll's name, preceded by a string of hash code(I suppose), which prevents windows search.

 You can use a small plugin called Everything to find them. The bundle files can be found in an installed Android package. Note that you have to log-in the game once and download the resources.

 Click "Info" once your open the bundle in UABE. Sort the files by size to make the following steps easier.

 2. Extract the texture.

 The largest file in the Bundle will generally be the texture. Export it with the .png plugin provided by UABE. You should be able to find 2 of them, for the T-dolls normal pose and destroyed pose.

 3. The moc3 file.

 They are usually the second-largest in the bundle. Export them as raw data. Use your hex editors to remove the file heading, so that it starts with "MOC3" in hex.

 After this, you should already be able to preview the model in Live2D viewer EX. This is a software that creates a model3 file automatically for you. You can create one by yourself and use other softwares to preview if the texture and model are correct.

 4. Raw motion and physics file.

 Find a bunch of CubismMotionFadeData(NOT animation clip) and export them as UABE json dump. You'd better seperately dump and convert normal/destroyed pose files as some of them have the same name.
 
 Then also dump CubismPhysicsController you find correspondingly. It's generally just slightly smaller than the motion files so it should be pretty close.

 ### Convert
 
 Put dumped motions inside GFLL2Dconv/input. Put physics controller in GFLL2Dconv/phy. Run the exe and you're all set.
 
 The files you get from here can be persumably used in any l2d players. I only tested it for CubismViewer and Live2DViewerEx.
