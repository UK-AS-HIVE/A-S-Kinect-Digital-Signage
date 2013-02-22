using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace HIVE_KinectGame
{
    /// <summary>
    /// The main type class for our videogame.
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        /// <summary>
        /// This region will define all of our global variables used throughout the game.
        /// </summary>
        #region VariableDefinitions

        /// <summary>
        /// Set up the graphics controllers for the environment
        /// </summary>
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        ContentManager content;

        /// <summary>
        /// Keyboard states for detecting keypressed
        /// </summary>
        private KeyboardState currentKeyboard;
        private KeyboardState previousKeyboard;

        /// <summary>
        /// Draw the simple planar grid for avatar to stand on if true.
        /// </summary>
        private bool drawGrid;

        /// <summary>
        /// This is the coordinate cross we use to draw the world coordinate system axes.
        /// </summary>
        private CoordinateCross worldAxes;

        /// <summary>
        /// Simple planar grid for avatar to stand on.
        /// </summary>
        private GridXz planarXzGrid;

        /// <summary>
        /// Camera Arc Increment value.
        /// </summary>
        private const float CameraArcIncrement = 0.1f;

        /// <summary>
        /// Camera Arc angle limit value.
        /// </summary>
        private const float CameraArcAngleLimit = 90.0f;

        /// <summary>
        /// Camera Zoom Increment value.
        /// </summary>
        private const float CameraZoomIncrement = 0.25f;

        /// <summary>
        /// Camera Max Distance value.
        /// </summary>
        private const float CameraMaxDistance = 500.0f;

        /// <summary>
        /// Camera Min Distance value.
        /// </summary>
        private const float CameraMinDistance = 15.0f;

        /// <summary>
        /// Camera starting Distance value.
        /// </summary>
        private const float CameraHeight = 40.0f;

        /// <summary>
        /// Camera starting Distance value.
        /// </summary>
        private const float CameraStartingTranslation = 40.0f;

        /// <summary>
        /// Viewing Camera arc.
        /// </summary>
        private float cameraArc = 0;

        /// <summary>
        /// Viewing Camera current rotation.
        /// The virtual camera starts where Kinect is looking i.e. looking along the Z axis, with +X left, +Y up, +Z forward
        /// </summary>
        private float cameraRotation = 0;

        /// <summary>
        /// Viewing Camera distance from origin.
        /// The "Dude" model is defined in centimeters, hence all the units we use here are cm.
        /// </summary>
        private float cameraDistance = CameraStartingTranslation;

        /// <summary>
        /// The "Dude" model mesh is defined at an arbitrary size in centimeters.
        /// Here we re-scale the Kinect translation so the model appears to walk more correctly on the ground.
        /// </summary>
        private static readonly Vector3 SkeletonTranslationScaleFactor = new Vector3(40.0f, 40.0f, 40.0f);

        /// <summary>
        /// The Kinect SDK can only actively track 2 skeletons at a time. This array holds the data
        /// corresponding to the two active skeletons.
        /// </summary>
        private Skeleton[] activeSkeletons = new Skeleton[2];

        /// <summary>
        /// The main kinect sensor
        /// </summary>
        private KinectSensor kinect = null;

        /// <summary>
        /// Image frames from the kinect
        /// </summary>
        ColorImageFrame colorFrame = null;
        DepthImageFrame depthFrame = null;

        /// <summary>
        /// Basic XNA effect for drawing on screen
        /// </summary>
        private BasicEffect effect; 

        /// <summary>
        /// Flag for screen mode. Full screen the game if true
        /// </summary>
        private Boolean isFullScreen = false;

        /// <summary>
        /// Set up basic screen sizes. We want to keep the aspect ratio standard 720 / 1080 HD video
        /// </summary>
        private const int WindowedWidth = 1280;
        private const int WindowedHeight = 720;
        private const int FullScreenWidth = 1920; // Change to 1920 for final
        private const int FullScreenHeight = 1080; // Change to 1080 for final

        /// <summary>
        /// This will be loaded with the 3D mesh of the avatars we will animate
        /// </summary>
        public Model[] avatars = new Model[2];

        /// <summary>
        /// Animators hold the data corresponding to how the mesh will be deformed for animation
        /// </summary>
        private AvatarAnimator[] animator = new AvatarAnimator[2];
        

        /// <summary>
        /// Map for the avatar mesh rigging to address bones in #AvatarRetargeting region
        /// </summary>
        private Dictionary<JointType, int> nuiJointToAvatarBoneIndex;

        /// <summary>
        /// Viewing Camera view matrix.
        /// </summary>
        private Microsoft.Xna.Framework.Matrix view;

        /// <summary>
        /// Viewing Camera projection matrix.
        /// </summary>
        private Microsoft.Xna.Framework.Matrix projection;

        /// <summary>
        /// Sets a seated posture when Seated Mode is on.
        /// </summary>
        private bool setSeatedPostureInSeatedMode;

        /// <summary>
        /// Fix the avatar hip center draw height.
        /// </summary>
        private bool fixAvatarHipCenterDrawHeight;

        /// <summary>
        /// Avatar hip center draw height.
        /// </summary>
        private float avatarHipCenterDrawHeight;

        /// <summary>
        /// Adjust Avatar lean when leaning back to reduce lean.
        /// </summary>
        private bool leanAdjust;

        /// <summary>
        /// Flag for controlling taking a photo of the players in the 3D environment. If true, we will take and save the image
        /// to the content folder. This should really remain false at all times except during the ONE cycle we want to take
        /// a snapshot-- else we will be continually taking photos.
        /// </summary>
        private Boolean takeScreencap = false;

        /// <summary>
        /// The screenshot/image number (a GUID) for writing the PNG file.
        /// </summary>
        private int snapNumber = 0;

        /// <summary>
        /// Random number generator
        /// </summary>
        private Random randomNum = new Random();

        /// <summary>
        /// Array of background images to display for the 3D environment.
        /// We will set up the total number of images dynamically later.
        /// Also, the splash screen image.
        /// </summary>
        private Texture2D[] envImages = null;
        private Texture2D backgroundImage = null;
        private Texture2D splashScreen = null;

        /// <summary>
        /// The array of slideshow images that we will look at during the show.
        /// </summary>
        private Texture2D[] slideshowImages = null;

        /// <summary>
        /// The final rendered image with the screenscreen processed onto it.
        /// </summary>
        private Texture2D colorVideo = null;

        /// <summary>
        /// The index of which slide in the slideshow we look at.
        /// </summary>
        private int whichSlide = 0;

        /// <summary>
        /// A placeholder for which environment image we will use
        /// </summary>
        private int whichEnv = 0;

        /// <summary>
        /// We have to change a few things each time we change scene, so this will keep track of scene
        /// changes. Mostly, we're concerned about being able to switch the background image in the 3D
        /// environment.
        /// </summary>
        private Boolean sceneJustChanged = false;

        /// <summary>
        /// Define the game state to control on screen display.
        ///   0 -> Intro graphic
        ///   1 -> Fade into the 3D environment
        ///   2 -> The 3D environment
        ///   3 -> The snapshot/screencap
        ///   4 -> Slideshow
        /// </summary>
        private int gameState = 0;

        /// <summary>
        /// A timer for controlling when events happen (i.e. when do we take snapshots and change the on
        /// screen display)
        /// </summary>
        double gameTimer = 0;

        /// <summary>
        /// Fading controls for changing screens etc.
        /// We only need to do the math if we want it, hence the flag.
        /// </summary>
        private int alphaValue = 255;
        private Boolean updateAlpha = false;
        private int fadeAmount = 10;

        /// <summary>
        /// Intermediate storage for the depth data received from the sensor
        /// </summary>
        private DepthImagePixel[] depthPixels = null;

        /// <summary>
        /// Intermediate storage for the green screen opacity mask
        /// </summary>
        private int[] greenScreenPixelData = null;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorImagePoint[] colorCoordinates = null;

        /// <summary>
        /// Inverse scaling factor between color and depth
        /// </summary>
        private int colorToDepthDivisor = 1;

        private Boolean foundPlayer = false;
        
        #endregion

        /// <summary>
        /// This section contains all information regarding initializing the game: loading content and starting up
        /// the game.
        /// </summary>
        #region GameInitializationLogic

        /// <summary>
        /// Main game entry
        /// </summary>
        public Game1()
        {
            // Set up the graphics to display
            graphics = new GraphicsDeviceManager(this);

            // Content directory. Here is where we will expect to find the 3D model and data, as well
            // as all imagery used in the project.
            Content.RootDirectory = "Content";

            /// Allows the game to run fullscreen
            content = new ContentManager(Services);

            // Set up resolution data for the final display.
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferMultiSampling = false;
            graphics.IsFullScreen = false;

            // Create a ground plane for the model to stand on
            this.planarXzGrid = new GridXz(this, new Vector3(0, 0, 0), new Vector2(500, 500), new Vector2(10, 10), Microsoft.Xna.Framework.Color.Black);
            this.Components.Add(this.planarXzGrid);
            this.drawGrid = false;

            // Generate the coordinate system for the world the avatar is in.
            this.worldAxes = new CoordinateCross(this, 500);
            this.Components.Add(this.worldAxes);

        }
        
        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Set up the kinect sensor. Using the first Kinect sensor available on the system
            // TODO: Add multi-kinect support in the future.
            kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

            // Enable the video stream, skeleton stream, and depth stream from our Kinect.
            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinect.SkeletonStream.Enable();
            kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            
            // Add a handler for when all frames are ready from the Kinect.
            // Data processing of the image, depth and skeleton frames will happen in this function.
            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);
            
            // If all goes well, intialize the Kinect!
            kinect.Start();
         
            // Set mouse visibility in the game.
            this.IsMouseVisible = true;

            // Drawing options for the avatar. Most likely these will stay false except in
            // certain circumstances. False for both enables free movement.
            this.setSeatedPostureInSeatedMode = false;
            this.leanAdjust = false;

            // Here we can force the avatar to be drawn at fixed height in the XNA virtual world.
            // The reason we may use this is because the sensor height above the physical floor
            // and the feet locations are not always known. Hence the avatar cannot be correctly 
            // placed on the ground plane or will be very jumpy.
            // Note: this will prevent the avatar from jumping and crouching.
            this.fixAvatarHipCenterDrawHeight = true;
            this.avatarHipCenterDrawHeight = 0.8f;  // in meters

            // Initialize our avatars and add them to the game.
            this.animator[0] = new AvatarAnimator(this, this.RetargetMatrixHierarchyToAvatarMesh, Game1.SkeletonTranslationScaleFactor);
            this.animator[1] = new AvatarAnimator(this, this.RetargetMatrixHierarchyToAvatarMesh, Game1.SkeletonTranslationScaleFactor);
            this.Components.Add(this.animator[0]);
            this.Components.Add(this.animator[1]);

            // Start it all up!
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of the content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create the XNA Basic Effect for line drawing
            this.effect = new BasicEffect(GraphicsDevice);
            if (null == this.effect)
            {
                throw new InvalidOperationException("Cannot load Basic Effect");
            }

            // Load in the avatar file into slots 0 and 1 of avatars. We are using the same
            // basic model/rigging for both active avatars.
            this.avatars[0] = Content.Load<Model>("dude");
            this.avatars[1] = Content.Load<Model>("dude");
            if (null == this.avatars[0])
            {
                throw new InvalidOperationException("Cannot load 3D avatar model");
            }

            // Load in the background images to place in the 3D environment. 
            // Discover all image files in the backgrounds folder (Content\backgrounds) and load them
            string[] envFiles = Directory.GetFiles(Content.RootDirectory + "\\" + "backgrounds");

            // Initialize our background image array to the number of images found.
            this.envImages = new Texture2D[envFiles.Length];

            // For each image we found in this directory, insert it into the array.
            for (int ctr = 0; ctr < envFiles.Length; ctr++)
            {
                using (FileStream stream = File.OpenRead(envFiles[ctr]))
                {
                    this.envImages[ctr] = Texture2D.FromStream(GraphicsDevice, stream);
                }
            }

            // Load in the splash screen image.
            FileStream splashStream = File.OpenRead(Content.RootDirectory + "\\splash.png");
            this.splashScreen = Texture2D.FromStream(GraphicsDevice, splashStream);

            // Load in the default background screen image.
            FileStream backStream = File.OpenRead(Content.RootDirectory + "\\asframe.png");
            this.backgroundImage = Texture2D.FromStream(GraphicsDevice, backStream);

            // Magic function that maps the joints to the avatar for animation.
            this.BuildJointHierarchy();

            // Add the models to the avatar animators and set where the hip will be
            // from earlier variables.
            this.animator[0].Avatar = this.avatars[0];
            this.animator[0].AvatarHipCenterHeight = this.avatarHipCenterDrawHeight;
            this.animator[1].Avatar = this.avatars[1];
            this.animator[1].AvatarHipCenterHeight = this.avatarHipCenterDrawHeight;
        }

        /// <summary>
        /// This function configures the mapping between the Nui Skeleton bones/joints and the Avatar bones/joints.
        /// Magic Microsoft-provided function.
        /// </summary>
        protected void BuildJointHierarchy()
        {
            // "Dude.fbx" bone index definitions
            // These are described as the "bone" that the transformation affects.
            // The rotation values are stored at the start joint before the bone (i.e. at the shared joint with the end of the parent bone).
            // 0 = root node
            // 1 = pelvis
            // 2 = spine
            // 3 = spine1
            // 4 = spine2
            // 5 = spine3
            // 6 = neck
            // 7 = head
            // 8-11 = eyes
            // 12 = Left clavicle (joint between spine and shoulder)
            // 13 = Left upper arm (joint at left shoulder)
            // 14 = Left forearm
            // 15 = Left hand
            // 16-30 = Left hand finger bones
            // 31 = Right clavicle (joint between spine and shoulder)
            // 32 = Right upper arm (joint at left shoulder)
            // 33 = Right forearm
            // 34 = Right hand
            // 35-49 = Right hand finger bones
            // 50 = Left Thigh
            // 51 = Left Knee
            // 52 = Left Ankle
            // 53 = Left Ball
            // 54 = Right Thigh
            // 55 = Right Knee
            // 56 = Right Ankle
            // 57 = Right Ball

            // For the Kinect NuiSkeleton, the joint at the end of the bone describes the rotation to get there, 
            // and the root orientation is in HipCenter. This is different to the Avatar skeleton described above.
            if (null == this.nuiJointToAvatarBoneIndex)
            {
                this.nuiJointToAvatarBoneIndex = new Dictionary<JointType, int>();
            }

            // Note: the actual hip center joint in the Avatar mesh has a root node (index 0) as well, which we ignore here for rotation.
            this.nuiJointToAvatarBoneIndex.Add(JointType.HipCenter, 1);
            this.nuiJointToAvatarBoneIndex.Add(JointType.Spine, 4);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ShoulderCenter, 6);
            this.nuiJointToAvatarBoneIndex.Add(JointType.Head, 7);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ElbowLeft, 13);
            this.nuiJointToAvatarBoneIndex.Add(JointType.WristLeft, 14);
            this.nuiJointToAvatarBoneIndex.Add(JointType.HandLeft, 15);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ElbowRight, 32);
            this.nuiJointToAvatarBoneIndex.Add(JointType.WristRight, 33);
            this.nuiJointToAvatarBoneIndex.Add(JointType.HandRight, 34);
            this.nuiJointToAvatarBoneIndex.Add(JointType.KneeLeft, 50);
            this.nuiJointToAvatarBoneIndex.Add(JointType.AnkleLeft, 51);
            this.nuiJointToAvatarBoneIndex.Add(JointType.FootLeft, 52);
            this.nuiJointToAvatarBoneIndex.Add(JointType.KneeRight, 54);
            this.nuiJointToAvatarBoneIndex.Add(JointType.AnkleRight, 55);
            this.nuiJointToAvatarBoneIndex.Add(JointType.FootRight, 56);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content
        /// </summary>x
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        #endregion

        /// <summary>
        /// This section contains all the runtime logic that we're concerned about.
        /// </summary>
        #region RunTimeLogic

        /// <summary>
        /// Event Handler method for recieving data frames from the Kinect.
        /// </summary>
        /// 
        protected void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs imageFrames)
        {
            // RGB Image Frame
            this.colorFrame = imageFrames.OpenColorImageFrame();

            // Depth Image Frame
            this.depthFrame = imageFrames.OpenDepthImageFrame();

            // If we have depth and image data, and we want to take a screenshot of the users in a particular environment.
            if (this.colorFrame != null && this.depthFrame != null && this.takeScreencap == true)
            {

                this.depthPixels = new DepthImagePixel[this.kinect.DepthStream.FramePixelDataLength];
                this.greenScreenPixelData = new int[this.kinect.DepthStream.FramePixelDataLength];
                this.colorCoordinates = new ColorImagePoint[this.kinect.DepthStream.FramePixelDataLength];

                this.depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                this.kinect.CoordinateMapper.MapDepthFrameToColorFrame(
                    DepthImageFormat.Resolution640x480Fps30,
                    this.depthPixels,
                    ColorImageFormat.RgbResolution640x480Fps30,
                    this.colorCoordinates);

                Array.Clear(greenScreenPixelData, 0, greenScreenPixelData.Length);

                //Create array for pixel data and copy it from the image frame
                Byte[] pixelData = new Byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixelData);

                //Convert RGBA to BGRA
                Byte[] bgraPixelData = new Byte[colorFrame.PixelDataLength];
                for (int i = 0; i < pixelData.Length; i += 4)
                {
                    bgraPixelData[i] = pixelData[i + 2];
                    bgraPixelData[i + 1] = pixelData[i + 1];
                    bgraPixelData[i + 2] = pixelData[i];
                    bgraPixelData[i + 3] = (Byte)255; //The video comes with 0 alpha so it is transparent
                }

                this.colorVideo = new Texture2D(this.graphics.GraphicsDevice, colorFrame.Width, colorFrame.Height);
                this.colorVideo.SetData(bgraPixelData);

                this.foundPlayer = false;
                // loop over each row and column of the depth
                for (int y = 0; y < 480; ++y)
                {
                    for (int x = 0; x < 640; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * 640);

                        DepthImagePixel depthPixel = depthPixels[depthIndex];

                        int player = depthPixel.PlayerIndex;

                        // if we're tracking a player for the current pixel, do green screen
                        if (player > 0)
                        {
                            this.foundPlayer = true;
                            // retrieve the depth to color mapping for the current depth pixel
                            ColorImagePoint colorImagePoint = this.colorCoordinates[depthIndex];

                            // scale color coordinates to depth resolution
                            int colorInDepthX = colorImagePoint.X / this.colorToDepthDivisor;
                            int colorInDepthY = colorImagePoint.Y / this.colorToDepthDivisor;

                            // make sure the depth pixel maps to a valid point in color space
                            // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                            // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                            // because of how the sensor works it is more correct to do it this way than to set to the right
                            if (colorInDepthX > 0 && colorInDepthX < 640 && colorInDepthY >= 0 && colorInDepthY < 480)
                            {
                                // calculate index into the green screen pixel array
                                int greenScreenIndex = colorInDepthX + (colorInDepthY * 640);

                                // set opaque
                                this.greenScreenPixelData[greenScreenIndex] = -1;

                                // compensate for depth/color not corresponding exactly by setting the pixel
                                // to the left to opaque as well
                                this.greenScreenPixelData[greenScreenIndex - 1] = -1;
                            }
                        }
                    }
                }

                // If we've found a player in the image, then run through the player mask
                // and copy pixels from the RGB camera to a final image
                // and save it as a screenshot.
                if (this.colorVideo != null && this.foundPlayer == true)
                {
                    
                     Stream stream = File.OpenWrite(this.Content.RootDirectory + "\\screenshots\\" + "snapshot-" + this.snapNumber + ".png");
                     //this.colorVideo.SaveAsJpeg(stream, 640, 480);
                     this.colorVideo.SaveAsPng(stream, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
                     this.snapNumber++;
                     stream.Close();
                     this.sceneJustChanged = true;
                  }
                    this.takeScreencap = false;
                    
            }


            // Process the skeleton stream
            using (SkeletonFrame skeletonFrame = imageFrames.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] allSkeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(allSkeletons);

                    // Clear out the activeSkeletons data. We don't want to keep it-- this allows dynamic on-the-fly user changes
                    // for groups of people.
                    activeSkeletons[0] = new Skeleton();
                    activeSkeletons[1] = new Skeleton();
                    activeSkeletons[0].TrackingId = -1;
                    activeSkeletons[1].TrackingId = -1;

                    // Add actively tracked skeletons to our list of activeSkeletons
                    foreach (Skeleton aSkeleton in allSkeletons)
                    {
                        if ((aSkeleton.TrackingState == SkeletonTrackingState.Tracked) && (activeSkeletons[0].TrackingId == -1))
                        {
                            activeSkeletons[0] = aSkeleton;
                        }

                        if ((aSkeleton.TrackingState == SkeletonTrackingState.Tracked) && (aSkeleton.TrackingId != activeSkeletons[0].TrackingId))
                        {
                            activeSkeletons[1] = aSkeleton;
                        }
                    }

                    // If we have a skeleton for the avatar, then set its visibility to true.
                    // Otherwise, make sure that we are hiding avatars that aren't attached to tracked people.
                    if (activeSkeletons[0].TrackingId != -1)
                    {
                        this.animator[0].CopySkeleton(activeSkeletons[0]);
                        this.animator[0].FloorClipPlane = skeletonFrame.FloorClipPlane;
                        this.animator[0].SkeletonVisible = true;
                    }
                    else { this.animator[0].SkeletonVisible = false; }

                    if (activeSkeletons[1].TrackingId != -1)
                    {
                        this.animator[1].CopySkeleton(activeSkeletons[1]);
                        this.animator[1].FloorClipPlane = skeletonFrame.FloorClipPlane;
                        this.animator[1].SkeletonVisible = true;
                    }
                    else { this.animator[1].SkeletonVisible = false; }
                }
            }
        }

        /// <summary>
        /// Runs all updating information for our game.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            // Update the avatar renderer. If we don't have actively tracked skeletons, then make sure we flag it.
            if (null != this.animator[0])
            {
                this.animator[0].SkeletonDrawn = false;
            }

            if (null != this.animator[1])
            {
                this.animator[1].SkeletonDrawn = false;
            }

            // Update saved keyboard state.
            this.previousKeyboard = this.currentKeyboard;
            this.currentKeyboard = Keyboard.GetState();

            // Check to see if keyboard state has changed or if still holding a key/not pressed
            if (this.previousKeyboard != this.currentKeyboard)
            {

                // Press Escape key to exit from game.
                if (this.currentKeyboard.IsKeyDown(Keys.Escape))
                {
                    this.Exit();
                }

                // Press F to fullscreen
                if (this.currentKeyboard.IsKeyDown(Keys.F))
                {
                    this.isFullScreen = !this.isFullScreen;
                    this.SetScreenMode();
                }

                // Manual override (mostly for testing)
                // Press S to take a picture
                if (this.currentKeyboard.IsKeyDown(Keys.S))
                {
                    this.takeScreencap = true;
                }

                // Manually adjust the tilt of the kinect sensor
                if (this.currentKeyboard.IsKeyDown(Keys.Up))
                {
                    if (kinect.ElevationAngle < (kinect.MaxElevationAngle + 5))
                    {
                        kinect.ElevationAngle += 5;
                    }
                }

                if (this.currentKeyboard.IsKeyDown(Keys.Down))
                {
                    if (kinect.ElevationAngle > (kinect.MinElevationAngle - 5))
                    {
                        kinect.ElevationAngle -= 5;
                    }
                }

            }

            // Update the game timer
            this.gameTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // Update alpha channel if we want to change it.
            if (this.updateAlpha)
            {
                this.alphaValue -= this.fadeAmount;
            }

            base.Update(gameTime);
            this.UpdateCamera(gameTime);
        }

        /// <summary>
        /// Create the viewing camera.
        /// </summary>
        protected void UpdateViewingCamera()
        {
            GraphicsDevice device = this.graphics.GraphicsDevice;

            // Compute camera matrices.
            this.view = Microsoft.Xna.Framework.Matrix.CreateTranslation(0, -CameraHeight, 0) *
                          Microsoft.Xna.Framework.Matrix.CreateRotationY(MathHelper.ToRadians(this.cameraRotation)) *
                          Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(this.cameraArc)) *
                          Microsoft.Xna.Framework.Matrix.CreateLookAt(
                                                new Vector3(0, 0, -this.cameraDistance),
                                                new Vector3(0, 0, 0),
                                                Vector3.Up);

            // Kinect vertical FOV in degrees
            float nominalVerticalFieldOfView = 45.6f;

            
            nominalVerticalFieldOfView = this.kinect.DepthStream.NominalVerticalFieldOfView;


            this.projection = Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(
                                                                nominalVerticalFieldOfView * (float)Math.PI / 180.0f,
                                                                device.Viewport.AspectRatio,
                                                                1,
                                                                10000);
        }

        /// <summary>
        /// Handles camera input.
        /// </summary>
        /// <param name="gameTime">The gametime.</param>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check for input to rotate the camera up and down around the model.
            if (this.currentKeyboard.IsKeyDown(Keys.Up) ||
                this.currentKeyboard.IsKeyDown(Keys.W))
            {
                this.cameraArc += time * CameraArcIncrement;
            }
            
            if (this.currentKeyboard.IsKeyDown(Keys.Down) ||
                this.currentKeyboard.IsKeyDown(Keys.S))
            {
                this.cameraArc -= time * CameraArcIncrement;
            }

            // Limit the arc movement.
            if (this.cameraArc > CameraArcAngleLimit)
            {
                this.cameraArc = CameraArcAngleLimit;
            }
            else if (this.cameraArc < -CameraArcAngleLimit)
            {
                this.cameraArc = -CameraArcAngleLimit;
            }

            // Check for input to rotate the camera around the model.
            if (this.currentKeyboard.IsKeyDown(Keys.Right) ||
                this.currentKeyboard.IsKeyDown(Keys.D))
            {
                this.cameraRotation += time * CameraArcIncrement;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.Left) ||
                this.currentKeyboard.IsKeyDown(Keys.A))
            {
                this.cameraRotation -= time * CameraArcIncrement;
            }

            // Check for input to zoom camera in and out.
            if (this.currentKeyboard.IsKeyDown(Keys.Z))
            {
                this.cameraDistance += time * CameraZoomIncrement;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.X))
            {
                this.cameraDistance -= time * CameraZoomIncrement;
            }

            // Limit the camera distance from the origin.
            if (this.cameraDistance > CameraMaxDistance)
            {
                this.cameraDistance = CameraMaxDistance;
            }
            else if (this.cameraDistance < CameraMinDistance)
            {
                this.cameraDistance = CameraMinDistance;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.R))
            {
                this.cameraArc = 0;
                this.cameraRotation = 0;
                this.cameraDistance = CameraStartingTranslation;
            }
        }

        /// <summary>
        /// Set fullscreen or windowed mode
        /// </summary>
        private void SetScreenMode()
        {
            // This sets the display resolution or window size to the desired size
            // If windowed, it also forces a 4:3 ratio for height and adds 110 for header/footer
            if (this.isFullScreen)
            {
                foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check our requested FullScreenWidth and Height against each supported display mode and set if valid
                    if ((mode.Width == FullScreenWidth) && (mode.Height == FullScreenHeight))
                    {
                        this.graphics.PreferredBackBufferWidth = FullScreenWidth;
                        this.graphics.PreferredBackBufferHeight = FullScreenHeight;
                        this.graphics.IsFullScreen = true;
                        this.graphics.ApplyChanges();
                    }
                }
            }
            else
            {
                if (WindowedWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                {
                    this.graphics.PreferredBackBufferWidth = WindowedWidth;
                    this.graphics.PreferredBackBufferHeight = 720;
                    this.graphics.IsFullScreen = false;
                    this.graphics.ApplyChanges();
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen so we don't have artifacts from updating.
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.White);
           
            #region DisplaySplashScreenState
            // If we are in the intro graphic screen. This will only happen at game startup.
            if (this.gameState == 0) 
            {
                if (this.updateAlpha == false && (this.gameTimer > 5))
                {
                    this.updateAlpha = true;
                }

                // Display the opening graphic.
                spriteBatch.Begin();
                spriteBatch.Draw(this.splashScreen, new Rectangle(0, 0, (graphics.PreferredBackBufferWidth), graphics.PreferredBackBufferHeight), new Microsoft.Xna.Framework.Color((byte)MathHelper.Clamp(this.alphaValue, 0, 255), (byte)MathHelper.Clamp(this.alphaValue, 0, 255), (byte)MathHelper.Clamp(this.alphaValue, 0, 255), (byte)MathHelper.Clamp(this.alphaValue, 0, 255)));
                spriteBatch.End();

                // Change gamestate to move into the 3D environment.
                if (this.alphaValue < 1)
                {
                    this.updateAlpha = false;
                    this.gameState = 1;
                    this.gameTimer = 0;
                }
            }
            #endregion

            #region Display3DEnvironmentState
            // If we are in the main 3D envrionment. This is the majority of the game.
            if (this.gameState == 1)
            {

                // If we are changing the 3D environment, then ensure we load a different background image than we just had.
                if (this.sceneJustChanged == true)
                {
                    int tempInt = this.whichEnv;
                    while (this.whichEnv == tempInt)
                    {
                        this.whichEnv = randomNum.Next(this.envImages.Length);
                    }

                    this.sceneJustChanged = false;
                }

                // Draw the background template
                if (this.backgroundImage != null)
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(this.backgroundImage, new Rectangle(0, 0, (graphics.PreferredBackBufferWidth), graphics.PreferredBackBufferHeight), Microsoft.Xna.Framework.Color.White);
                    spriteBatch.End();
                }

                // Draw the environment images.
                if (this.envImages != null)
                {
                    if (this.isFullScreen)
                    {
                        spriteBatch.Begin();
                        // Hard-coded values are bad.
                        spriteBatch.Draw(this.envImages[this.whichEnv], new Rectangle(553, 44, 1326, 997), Microsoft.Xna.Framework.Color.White);
                        spriteBatch.End();
                    }
                    else
                    {
                        spriteBatch.Begin();
                        // Windowed mode is forcing 1280x720....
                        spriteBatch.Draw(this.envImages[this.whichEnv], new Rectangle(369, 29, 884, 664), Microsoft.Xna.Framework.Color.White);
                        spriteBatch.End();
                    }
                }

                // Update the viewing camera.
                this.UpdateViewingCamera();

                // Draw the world grid if we need it.
                if (this.drawGrid && null != this.planarXzGrid && null != this.worldAxes)
                {
                    this.planarXzGrid.Draw(gameTime, Microsoft.Xna.Framework.Matrix.Identity, this.view, this.projection);
                    this.worldAxes.Draw(gameTime, Microsoft.Xna.Framework.Matrix.Identity, this.view, this.projection);
                }

                // Draw the actual avatars in the 3D environment
                if (activeSkeletons[0].TrackingId > -1)
                {
                    this.animator[0].Draw(gameTime, Microsoft.Xna.Framework.Matrix.Identity, this.view, this.projection);

                    if (activeSkeletons[1].TrackingId > -1)
                    {
                        this.animator[1].Draw(gameTime, Microsoft.Xna.Framework.Matrix.Identity, this.view, this.projection);
                    }
                }

                // If we have taken 5 snaps, then go to the slideshow.
                if (this.snapNumber % 5 == 0 && this.snapNumber > 0)
                {
                    // Okay we need to add one to snapNumber when we get to this point
                    // otherwise it will get stuck in an infinite loop. 
                    // There is a more graceful way to do this, but I'm crunched on time.
                    this.snapNumber++;
                    this.gameState = 2;
                    this.gameTimer = 0;
                }

                // If we've been on this particular 3D scene for 10 seconds, take a snap and change it!
                if (gameTimer > 5)
                {
                    // Make sure to reset the game timer.
                    this.gameTimer = 0;
                    this.takeScreencap = true;
                }
            }
            #endregion

            #region DisplaySlideshowState
            // If gameState is 2, then go to the slideshow display.
            if (this.gameState == 2)
            {
                GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
                // Load in the background images to place in the 3D environment. 
                // Discover all image files in the backgrounds folder (Content\backgrounds) and load them
                string[] slideshowFiles = Directory.GetFiles(Content.RootDirectory + "\\" + "screenshots");

                if (this.sceneJustChanged == true)
                {
                    int tempInt = this.whichSlide;
                    while (this.whichSlide == tempInt)
                    {
                        this.whichSlide = randomNum.Next(slideshowFiles.Length);
                    }

                    this.sceneJustChanged = false;
                }

                // Initialize our background image array to the number of images found.
                this.slideshowImages = new Texture2D[slideshowFiles.Length];

                // For each image we found in this directory, insert it into the array.
                for (int ctr = 0; ctr < this.slideshowImages.Length; ctr++)
                {
                    using (FileStream stream = File.OpenRead(slideshowFiles[ctr]))
                    {
                        this.slideshowImages[ctr] = Texture2D.FromStream(GraphicsDevice, stream);
                    }
                }

                // Draw the background images.
                if (backgroundImage != null)
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(this.backgroundImage, new Rectangle(0, 0, (graphics.PreferredBackBufferWidth), graphics.PreferredBackBufferHeight), Microsoft.Xna.Framework.Color.White);
                    spriteBatch.End();
                }

                if (this.slideshowImages != null)
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(this.slideshowImages[this.whichSlide], new Rectangle(369, 29, 884, 664), Microsoft.Xna.Framework.Color.White);
                    spriteBatch.End();
                }

                // If we've looked at this image for a few seconds, change it.
                if (Math.Floor(this.gameTimer) % 2 == 0)
                {
                    this.sceneJustChanged = true;
                }

                if (this.gameTimer > 6)
                {
                    this.gameState = 0;
                    this.gameTimer = 0;
                    this.alphaValue = 255;
                }

                // When we go back to the 3D environment, make sure we load a new background image.
                this.sceneJustChanged = true;
            }

            #endregion
            // Draw RGB video
            /*if (this.colorVideo != null && this.takeScreencap == true)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(this.colorVideo, new Rectangle(0, 0, 640, 480), Microsoft.Xna.Framework.Color.White);
                spriteBatch.End();
            } */
            base.Draw(gameTime);
        }

        #endregion

        /// <summary>
        /// Microsoft-provided methods for correcting avatar/user tracking and making it display well on screen.
        /// </summary>
        #region AvatarRetargeting

        /// <summary>
        /// 3D avatar models typically have varying bone structures and joint orientations, depending on how they are built.
        /// Here we adapt the calculated hierarchical relative rotation matrices to work with our avatar and set these into the 
        /// boneTransforms array. This array is then later converted to world transforms and then skinning transforms for the
        /// XNA skinning processor to draw the mesh.
        /// The "Dude.fbx" model defines more bones/joints (57 in total) and in different locations and orientations to the 
        /// Nui Skeleton. Many of the bones/joints have no direct equivalent - e.g. with Kinect we cannot currently recover 
        /// the fingers pose. Bones are defined relative to each other, hence unknown bones will be left as identity relative
        /// transformation in the boneTransforms array, causing them to take their parent's orientation in the world coordinate system.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="bindRoot">The bind root matrix of the avatar mesh.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void RetargetMatrixHierarchyToAvatarMesh(Skeleton skeleton, Microsoft.Xna.Framework.Matrix bindRoot, Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            if (null == skeleton)
            {
                return;
            }

            // Set the bone orientation data in the avatar mesh
            foreach (BoneOrientation bone in skeleton.BoneOrientations)
            {
                // If any of the joints/bones are not tracked, skip them
                // Note that if we run filters on the raw skeleton data, which fix tracking problems,
                // We should set the tracking state from NotTracked to Inferred.
                if (skeleton.Joints[bone.EndJoint].TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                this.SetJointTransformation(bone, skeleton, bindRoot, ref boneTransforms);
            }

            // If seated mode is on, sit the avatar down
            if (this.setSeatedPostureInSeatedMode)
            {
                this.SetSeatedPosture(ref boneTransforms);
            }

            // Set the world position of the avatar
            this.SetAvatarRootWorldPosition(skeleton, ref boneTransforms);
        }

        /// <summary>
        /// Set the bone transform in the avatar mesh.
        /// </summary>
        /// <param name="bone">Nui Joint/bone orientation</param>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="bindRoot">The bind root matrix of the avatar mesh.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetJointTransformation(BoneOrientation bone, Skeleton skeleton, Microsoft.Xna.Framework.Matrix bindRoot, ref Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            // Always look at the skeleton root
            if (bone.StartJoint == JointType.HipCenter && bone.EndJoint == JointType.HipCenter)
            {
                // Unless in seated mode, the hip center is special - it is the root of the NuiSkeleton and describes the skeleton orientation in the world
                // (camera) coordinate system. All other bones/joint orientations in the hierarchy have hip center as one of their parents.
                // However, if in seated mode, the shoulder center then holds the skeleton orientation in the world (camera) coordinate system.
                bindRoot.Translation = Vector3.Zero;
                Microsoft.Xna.Framework.Matrix invBindRoot = Microsoft.Xna.Framework.Matrix.Invert(bindRoot);

                Microsoft.Xna.Framework.Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Here we create a rotation matrix for the hips from the inverse of the bind pose
                // for the pelvis rotation and the inverse of the bind pose for the root node (0) in the Dude model.
                // This multiplication effectively removes the initial 90 degree rotations set in the first two model nodes.
                Microsoft.Xna.Framework.Matrix pelvis = boneTransforms[1];
                pelvis.Translation = Vector3.Zero; // Ensure pure rotation as we explicitly set world translation from the Kinect camera below.
                Microsoft.Xna.Framework.Matrix invPelvis = Microsoft.Xna.Framework.Matrix.Invert(pelvis);

                Microsoft.Xna.Framework.Matrix combined = (invBindRoot * hipOrientation) * invPelvis;

                this.ReplaceBoneMatrix(JointType.HipCenter, combined, true, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ShoulderCenter)
            {
                // This contains an absolute rotation if we are in seated mode, or the hip center is not tracked, as the HipCenter will be identity
                /*
                if (this.chooser.SeatedMode || (this.Chooser.SeatedMode == false && skeleton.Joints[JointType.HipCenter].TrackingState == JointTrackingState.NotTracked))
                {
                    bindRoot.Translation = Vector3.Zero;
                    Matrix invBindRoot = Matrix.Invert(bindRoot);

                    Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                    // We can use the same method as in HipCenter above to invert the root and pelvis bind pose,
                    // however, alternately we can also explicitly swap axes and adjust the rotations to get from
                    // the Kinect rotation to the model hip orientation, similar to what we do for the following joints/bones.

                    // Kinect = +X left, +Y up, +Z forward in body coordinate system
                    // Avatar = +Z left, +X up, +Y forward
                    Quaternion kinectRotation = KinectHelper.DecomposeMatRot(hipOrientation);    // XYZ
                    Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                    Matrix combined = Matrix.CreateFromQuaternion(avatarRotation);

                    // Add a small adjustment rotation to manually correct for the rotation in the parent bind
                    // pose node in the model mesh - this can be found by looking in the FBX or in 3DSMax/Maya.
                    Matrix adjustment = Matrix.CreateRotationY(MathHelper.ToRadians(-90));
                    combined *= adjustment;
                    Matrix adjustment2 = Matrix.CreateRotationZ(MathHelper.ToRadians(-90));
                    combined *= adjustment2;

                    // Although not strictly correct, we apply this to the hip center, as all other bones are children of this joint.
                    // Application at the spine or shoulder center instead would require manually updating of the bone orientations below for the whole body to move when the shoulders twist or tilt.
                    this.ReplaceBoneMatrix(JointType.HipCenter, combined, true, ref boneTransforms);
                }*/
            }
            else if (bone.EndJoint == JointType.Spine)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // The Dude appears to lean back too far compared to a real person, so here we adjust this lean.
                this.CorrectBackwardsLean(skeleton, ref tempMat);

                // Also add a small constant adjustment rotation to correct for the hip center to spine bone being at a rear-tilted angle in the Kinect skeleton.
                // The dude should now look more straight ahead when avateering
                Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(20));  // 20 degree rotation around the local Kinect x axis for the spine bone.
                tempMat *= adjustment;

                // Kinect = +X left, +Y up, +Z forward in body coordinate system
                // Avatar = +Z left, +X up, +Y forward
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                // Set the corresponding matrix in the avatar using the translation table we specified.
                // Note for the spine and shoulder center rotations, we could also try to spread the angle
                // over all the Avatar skeleton spine joints, causing a more curved back, rather than apply
                // it all to one joint, as we do here.
                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.Head)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton head bones being defined pointing looking slightly down, not vertical.
                // The dude should now look more straight ahead when avateering
                Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(-30));  // -30 degree rotation around the local Kinect x axis for the head bone.
                tempMat *= adjustment;

                // Kinect = +X left, +Y up, +Z forward in body coordinate system
                // Avatar = +Z left, +X up, +Y forward
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                // Set the corresponding matrix in the avatar using the translation table we specified
                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ElbowLeft || bone.EndJoint == JointType.WristLeft)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                if (bone.EndJoint == JointType.ElbowLeft)
                {
                    // Add a small adjustment rotation to correct for the avatar skeleton shoulder/upper arm bones.
                    // The dude should now be able to have arms correctly down at his sides when avateering
                    Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationZ(MathHelper.ToRadians(-15));  // -15 degree rotation around the local Kinect z axis for the upper arm bone.
                    tempMat *= adjustment;
                }

                // Kinect = +Y along arm, +X down, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z backwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.Z, -kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.HandLeft)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton wist/hand bone.
                // The dude should now have the palm of his hands toward his body when arms are straight down
                Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationY(MathHelper.ToRadians(-90));  // -90 degree rotation around the local Kinect y axis for the wrist-hand bone.
                tempMat *= adjustment;

                // Kinect = +Y along arm, +X down, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z backwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.X, -kinectRotation.Z, kinectRotation.W);
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ElbowRight || bone.EndJoint == JointType.WristRight)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                if (bone.EndJoint == JointType.ElbowRight)
                {
                    // Add a small adjustment rotation to correct for the avatar skeleton shoulder/upper arm bones.
                    // The dude should now be able to have arms correctly down at his sides when avateering
                    Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationZ(MathHelper.ToRadians(15));  // 15 degree rotation around the local Kinect  z axis for the upper arm bone.
                    tempMat *= adjustment;
                }

                // Kinect = +Y along arm, +X up, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y back, +Z down
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.Z, -kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.HandRight)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton wist/hand bone.
                // The dude should now have the palm of his hands toward his body when arms are straight down
                Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationY(MathHelper.ToRadians(90));  // -90 degree rotation around the local Kinect y axis for the wrist-hand bone.
                tempMat *= adjustment;

                // Kinect = +Y along arm, +X up, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z forwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.X, kinectRotation.Z, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.KneeLeft)
            {
                // Combine the two joint rotations from the hip and knee
                Microsoft.Xna.Framework.Matrix hipLeft = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipLeft].HierarchicalRotation.Matrix);
                Microsoft.Xna.Framework.Matrix kneeLeft = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                Microsoft.Xna.Framework.Matrix combined = kneeLeft * hipLeft;

                this.SetLegMatrix(bone.EndJoint, combined, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.AnkleLeft || bone.EndJoint == JointType.AnkleRight)
            {
                Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                this.SetLegMatrix(bone.EndJoint, tempMat, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.KneeRight)
            {
                // Combine the two joint rotations from the hip and knee
                Microsoft.Xna.Framework.Matrix hipRight = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipRight].HierarchicalRotation.Matrix);
                Microsoft.Xna.Framework.Matrix kneeRight = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                Microsoft.Xna.Framework.Matrix combined = kneeRight * hipRight;

                this.SetLegMatrix(bone.EndJoint, combined, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.FootLeft || bone.EndJoint == JointType.FootRight)
            {
                // Only set this if we actually have a good track on this and the parent
                if (skeleton.Joints[bone.EndJoint].TrackingState == JointTrackingState.Tracked && skeleton.Joints[skeleton.BoneOrientations[bone.EndJoint].StartJoint].TrackingState == JointTrackingState.Tracked)
                {
                    Microsoft.Xna.Framework.Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                    // Add a small adjustment rotation to correct for the avatar skeleton foot bones being defined pointing down at 45 degrees, not horizontal
                    Microsoft.Xna.Framework.Matrix adjustment = Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(-45));
                    tempMat *= adjustment;

                    // Kinect = +Y along foot (fwd), +Z up, +X right in body coordinate system
                    // Avatar = +X along foot (fwd), +Y up, +Z right
                    Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat); // XYZ
                    Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                    tempMat = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

                    this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
                }
            }
        }

        /// <summary>
        /// Correct the spine rotation when leaning back to reduce lean.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="spineMat">The spine orientation.</param>
        private void CorrectBackwardsLean(Skeleton skeleton, ref Microsoft.Xna.Framework.Matrix spineMat)
        {
            Microsoft.Xna.Framework.Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipCenter].HierarchicalRotation.Matrix);

            Vector3 hipZ = new Vector3(hipOrientation.M31, hipOrientation.M32, hipOrientation.M33);   // Z (forward) vector
            Vector3 boneY = new Vector3(spineMat.M21, spineMat.M22, spineMat.M23);   // Y (up) vector

            hipZ *= -1;
            hipZ.Normalize();
            boneY.Normalize();

            // Dot product the hip center forward vector with our spine bone up vector.
            float cosAngle = Vector3.Dot(hipZ, boneY);

            // If it's negative (i.e. greater than 90), we are leaning back, so reduce this lean.
            if (cosAngle < 0 && this.leanAdjust)
            {
                float angle = (float)Math.Acos(cosAngle);
                float correction = (angle / 2) * -(cosAngle / 2);
                Microsoft.Xna.Framework.Matrix leanAdjustment = Microsoft.Xna.Framework.Matrix.CreateRotationX(correction);  // reduce the lean by up to half, scaled by how far back we are leaning
                spineMat *= leanAdjustment;
            }
        }

        /// <summary>
        /// Helper used for leg bones.
        /// </summary>
        /// <param name="joint">Nui Joint index</param>
        /// <param name="legRotation">Matrix containing a leg joint rotation.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetLegMatrix(JointType joint, Microsoft.Xna.Framework.Matrix legRotation, ref Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            // Kinect = +Y along leg (down), +Z fwd, +X right in body coordinate system
            // Avatar = +X along leg (down), +Y fwd, +Z right
            Quaternion kinectRotation = KinectHelper.DecomposeMatRot(legRotation);  // XYZ
            Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
            legRotation = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(avatarRotation);

            this.ReplaceBoneMatrix(joint, legRotation, false, ref boneTransforms);
        }

        /// <summary>
        /// Set the avatar root position in world coordinates.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetAvatarRootWorldPosition(Skeleton skeleton, ref Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            // Get XNA world position of skeleton.
            Microsoft.Xna.Framework.Matrix worldTransform = this.GetModelWorldTranslation(skeleton.Joints, false);

            // set root translation
            boneTransforms[0].Translation = worldTransform.Translation;
        }

        /// <summary>
        /// This function sets the mapping between the Nui Skeleton bones/joints and the Avatar bones/joints
        /// </summary>
        /// <param name="joint">Nui Joint index</param>
        /// <param name="boneMatrix">Matrix to set in joint/bone.</param>
        /// <param name="replaceTranslationInExistingBoneMatrix">set Boolean true to replace the translation in the original bone matrix with the one passed in boneMatrix (i.e. at root), false keeps the original (default).</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void ReplaceBoneMatrix(JointType joint, Microsoft.Xna.Framework.Matrix boneMatrix, bool replaceTranslationInExistingBoneMatrix, ref Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            int meshJointId;
            bool success = this.nuiJointToAvatarBoneIndex.TryGetValue(joint, out meshJointId);

            if (success)
            {
                Vector3 offsetTranslation = boneTransforms[meshJointId].Translation;
                boneTransforms[meshJointId] = boneMatrix;

                if (replaceTranslationInExistingBoneMatrix == false)
                {
                    // overwrite any new boneMatrix translation with the original one
                    boneTransforms[meshJointId].Translation = offsetTranslation;   // re-set the translation
                }
            }
        }

        /// <summary>
        /// Helper used to get the world translation for the root.
        /// </summary>
        /// <param name="joints">Nui Joint collection.</param>
        /// <param name="seatedMode">Boolean true if seated mode.</param>
        /// <returns>Returns a Matrix containing the translation.</returns>
        private Microsoft.Xna.Framework.Matrix GetModelWorldTranslation(JointCollection joints, bool seatedMode)
        {
            Vector3 transVec = Vector3.Zero;

            if (seatedMode && joints[JointType.ShoulderCenter].TrackingState != JointTrackingState.NotTracked)
            {
                transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.ShoulderCenter].Position);
            }
            else
            {
                if (joints[JointType.HipCenter].TrackingState != JointTrackingState.NotTracked)
                {
                    transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.HipCenter].Position);
                }
                else if (joints[JointType.ShoulderCenter].TrackingState != JointTrackingState.NotTracked)
                {
                    // finally try shoulder center if this is tracked while hip center is not
                    transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.ShoulderCenter].Position);
                }
            }

            if (this.fixAvatarHipCenterDrawHeight)
            {
                transVec.Y = this.avatarHipCenterDrawHeight;
            }

            // Here we scale the translation, as the "Dude" avatar mesh is defined in centimeters, and the Kinect skeleton joint positions in meters.
            return Microsoft.Xna.Framework.Matrix.CreateTranslation(transVec * SkeletonTranslationScaleFactor);
        }

        /// <summary>
        /// Sets the Avatar in a seated posture - useful for seated mode.
        /// </summary>
        /// <param name="boneTransforms">The relative bone transforms of the avatar mesh.</param>
        private void SetSeatedPosture(ref Microsoft.Xna.Framework.Matrix[] boneTransforms)
        {
            // In the Kinect coordinate system, we first rotate from the local avatar 
            // root orientation with +Y up to +Y down for the leg bones (180 around Z)
            // then pull the knees up for a seated posture.
            Microsoft.Xna.Framework.Matrix rot180 = Microsoft.Xna.Framework.Matrix.CreateRotationZ(MathHelper.ToRadians(180));
            Microsoft.Xna.Framework.Matrix rot90 = Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(90));
            Microsoft.Xna.Framework.Matrix rotMinus90 = Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(-90));
            Microsoft.Xna.Framework.Matrix combinedHipRotation = rot90 * rot180;

            this.SetLegMatrix(JointType.KneeLeft, combinedHipRotation, ref boneTransforms);
            this.SetLegMatrix(JointType.KneeRight, combinedHipRotation, ref boneTransforms);
            this.SetLegMatrix(JointType.AnkleLeft, rotMinus90, ref boneTransforms);
            this.SetLegMatrix(JointType.AnkleRight, rotMinus90, ref boneTransforms);
        }

        #endregion
    }
}

