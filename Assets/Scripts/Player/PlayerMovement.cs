/*
	Created by @DawnosaurDev at youtube.com/c/DawnosaurStudios
	Thanks so much for checking this out and I hope you find it helpful! 
	If you have any further queries, questions or feedback feel free to reach out on my twitter or leave a comment on youtube :D

	Feel free to use this in your own games, and I'd love to see anything you make!
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Player
{
	public class PlayerMovement : MonoBehaviour
	{
		public static PlayerMovement Instance { get; private set; }
		
		[Header("Gravity")]
		[HideInInspector] public float gravityStrength; //Downwards force (gravity) needed for the desired jumpHeight and jumpTimeToApex.
		[HideInInspector] public float gravityScale; //Strength of the player's gravity as a multiplier of gravity (set in ProjectSettings/Physics2D).
		//Also the value the player's rigidbody2D.gravityScale is set to.
		[Space(5)]
		public float fallGravityMult; //Multiplier to the player's gravityScale when falling.
		public float maxFallSpeed; //Maximum fall speed (terminal velocity) of the player when falling.
		[Space(5)]
		public float fastFallGravityMult; //Larger multiplier to the player's gravityScale when they are falling and a downwards input is pressed.
		//Seen in games such as Celeste, lets the player fall extra fast if they wish.
		public float maxFastFallSpeed; //Maximum fall speed(terminal velocity) of the player when performing a faster fall.
	
		[Space(20)]

		[Header("Run")]
		public float runMaxSpeed; //Target speed we want the player to reach.
		public float runAcceleration; //The speed at which our player accelerates to max speed, can be set to runMaxSpeed for instant acceleration down to 0 for none at all
		[HideInInspector] public float runAccelAmount; //The actual force (multiplied with speedDiff) applied to the player.
		public float runDecceleration; //The speed at which our player decelerates from their current speed, can be set to runMaxSpeed for instant deceleration down to 0 for none at all
		[HideInInspector] public float runDeccelAmount; //Actual force (multiplied with speedDiff) applied to the player .
		[Space(5)]
		[Range(0f, 1)] public float accelInAir; //Multipliers applied to acceleration rate when airborne.
		[Range(0f, 1)] public float deccelInAir;
		[Space(5)]
		public bool doConserveMomentum = true;

		[Space(20)]

		[Header("Jump")]
		public float jumpHeight; //Height of the player's jump
		public float jumpTimeToApex; //Time between applying the jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
		[HideInInspector] public float jumpForce; //The actual force applied (upwards) to the player when they jump.

		[Header("Both Jumps")]
		public float jumpCutGravityMult; //Multiplier to increase gravity if the player releases thje jump button while still jumping
		[Range(0f, 1)] public float jumpHangGravityMult; //Reduces gravity while close to the apex (desired max height) of the jump
		public float jumpHangTimeThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)
		[Space(0.5f)]
		public float jumpHangAccelerationMult; 
		public float jumpHangMaxSpeedMult;

		[Header("Wall Jump")]
		public float wallJumpHorizontalForce; //The actual force (this time set by us) applied to the player when wall jumping.
		public float wallJumpVerticalMult;
		[Space(5)]
		[Range(0f, 1f)] public float wallJumpRunLerp; //Reduces the effect of player's movement while wall jumping.
		[Range(0f, 1.5f)] public float wallJumpTime; //Time after wall jumping the player's movement is slowed for.
		public bool doTurnOnWallJump; //Player will rotate to face wall jumping direction

		[Space(20)]

		[Header("Slide")]
		public float slideSpeed;
		public float slideAccel;

		[Header("Assists")]
		[Range(0.01f, 0.5f)] public float coyoteTime; //Grace period after falling off a platform, where you can still jump
		[Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.

		[Space(20)]

		[Header("Dash")]
		public float dashSpeed;
		public float dashSleepTime; //Duration for which the game freezes when we press dash but before we read directional input and apply a force
		[Space(5)]
		public float dashAttackTime;
		[Space(5)]
		public float dashEndTime; //Time after you finish the inital drag phase, smoothing the transition back to idle (or any standard state)
		public Vector2 dashEndSpeed; //Slows down player, makes dash feel more responsive (used in Celeste)
		[Range(0f, 1f)] public float dashEndRunLerp; //Slows the affect of player movement while dashing
		[Space(5)]
		public float dashRefillTime;
		[Space(5)]
		[Range(0.01f, 0.5f)] public float dashInputBufferTime;

		private float percent;
		private float timeScale;
		
		public AnimationCurve slowTimeCurve;
		public AnimationCurve speedUpTimeCurve;
		public float speedUpTime;

		#region COMPONENTS
		public Rigidbody2D RB { get; private set; }
		//Script to handle all player animations, all references can be safely removed if you're importing into your own project.
		//public PlayerAnimator AnimHandler { get; private set; }

		public SpriteRenderer bubbleSprite;
		public SpriteRenderer playerSprite;

		public List<Image> bubbleUiImages;

		#endregion

		#region STATE PARAMETERS
		//Variables control the various actions the player can perform at any time.
		//These are fields which can are public allowing for other sctipts to read them
		//but can only be privately written to.
		public bool IsFacingRight { get; private set; }
		public bool IsJumping { get; private set; }
		public bool IsWallJumping { get; private set; }
		public bool IsDashing { get; private set; }
		public bool IsSliding { get; private set; }

		//Timers (also all fields, could be private and a method returning a bool could be used)
		public float LastOnGroundTime { get; private set; }
		public float LastOnWallTime { get; private set; }
		public float LastOnWallRightTime { get; private set; }
		public float LastOnWallLeftTime { get; private set; }

		//Jump
		private bool _isJumpCut;
		private bool _isJumpFalling;

		//Wall Jump
		private float _wallJumpStartTime;
		private int _lastWallJumpDir;

		//Dash
		private int _dashesLeft;
		private bool _dashRefilling;
		private bool _dashCanRefill; //set when leaving refill zone
		private Vector2 _lastDashDir;
		private bool _isDashAttacking;

		#endregion

		#region INPUT PARAMETERS
		private Vector2 _moveInput;

		public float LastPressedJumpTime { get; private set; }
		public float LastPressedDashTime { get; private set; }
		#endregion

		#region CHECK PARAMETERS
		//Set all of these up in the inspector
		[Header("Checks")] 
		[SerializeField] private Transform _groundCheckPoint;
		//Size of groundCheck depends on the size of your character generally you want them slightly small than width (for ground) and height (for the wall check)
		[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
		[Space(5)]
		[SerializeField] private Transform _frontWallCheckPoint;
		[SerializeField] private Transform _backWallCheckPoint;
		[SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
		#endregion

		#region LAYERS & TAGS
		[Header("Layers & Tags")]
		[SerializeField] private LayerMask _physicsCheckLayer;
		#endregion

		//Unity Callback, called when the inspector updates
		private void OnValidate()
		{
			//Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2) 
			gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
		
			//Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics2D)
			gravityScale = gravityStrength / Physics2D.gravity.y;

			//Calculate are run acceleration & deceleration forces using formula: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
			runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
			runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

			//Calculate jumpForce using the formula (initialJumpVelocity = gravity * timeToJumpApex)
			jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

			#region Variable Ranges
			runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
			runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
			#endregion
		}

		private void Awake()
		{
			RB = GetComponent<Rigidbody2D>();
			//AnimHandler = GetComponent<PlayerAnimator>();

			if (Instance != null)
			{
				Debug.LogError("More than one PlayerMovement in scene!");
			}
			Instance = this;
		}

		private void Start()
		{
			SetGravityScale(gravityScale);
			IsFacingRight = true;

			bubbleSprite.enabled = false;
			playerSprite.enabled = true;
		}
		

		private void Update()
		{
			#region TIMERS
			LastOnGroundTime -= Time.deltaTime;
			LastOnWallTime -= Time.deltaTime;
			LastOnWallRightTime -= Time.deltaTime;
			LastOnWallLeftTime -= Time.deltaTime;

			LastPressedJumpTime -= Time.deltaTime;
			LastPressedDashTime -= Time.deltaTime;
			#endregion

			#region INPUT HANDLER

			_moveInput = UserInput.Instance.MoveInput;

			if (_moveInput.x != 0)
				CheckDirectionToFace(_moveInput.x > 0);

			if(UserInput.Instance.JumpButtonPressedThisFrame)
			{
				OnJumpInput();
			}

			if (UserInput.Instance.JumpButtonReleasedThisFrame)
			{
				OnJumpUpInput();
			}

			if (UserInput.Instance.JumpButtonHoldPerformed)
			{
				OnDashInput();
			}

			// if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
			// {
			// 	OnDashInput();
			// }
			#endregion

			#region COLLISION CHECKS
			if (!IsDashing && !IsJumping)
			{
				//Ground Check
				if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _physicsCheckLayer)) //checks if set box overlaps with ground
				{
					if(LastOnGroundTime < -0.1f)
					{
						//TODO: mecanim
						//AnimHandler.justLanded = true;
					}

					LastOnGroundTime = coyoteTime; //if so sets the lastGrounded to coyoteTime
				}		

				//Right Wall Check
				if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _physicsCheckLayer) && IsFacingRight)
				     || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _physicsCheckLayer) && !IsFacingRight)) && !IsWallJumping)
					LastOnWallRightTime = coyoteTime;

				//Right Wall Check
				if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _physicsCheckLayer) && !IsFacingRight)
				     || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _physicsCheckLayer) && IsFacingRight)) && !IsWallJumping)
					LastOnWallLeftTime = coyoteTime;

				//Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
				LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
			}
			#endregion

			#region JUMP CHECKS
			if (IsJumping && RB.linearVelocity.y <= 0)
			{
				IsJumping = false;

				_isJumpFalling = true;
			}

			if (IsWallJumping && Time.time - _wallJumpStartTime > wallJumpTime)
			{
				IsWallJumping = false;
			}

			if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
			{
				_isJumpCut = false;

				_isJumpFalling = false;
			}

			if (!IsDashing)
			{
				//Jump
				if (CanJump() && LastPressedJumpTime > 0)
				{
					IsJumping = true;
					IsWallJumping = false;
					_isJumpCut = false;
					_isJumpFalling = false;
					Jump();

					//TODO: mecanim
					// AnimHandler.startedJumping = true;
				}
				//WALL JUMP
				else if (CanWallJump() && LastPressedJumpTime > 0)
				{
					IsWallJumping = true;
					IsJumping = false;
					_isJumpCut = false;
					_isJumpFalling = false;

					_wallJumpStartTime = Time.time;
					_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

					WallJump(_lastWallJumpDir);
				}
			}
			#endregion

			#region DASH CHECKS
			if (CanDash() && LastPressedDashTime > 0)
			{
				//Freeze game for split second. Adds juiciness and a bit of forgiveness over directional input
				bubbleSprite.enabled = true;

				//If not direction pressed, dash forward
				if (_moveInput != Vector2.zero)
					_lastDashDir = _moveInput;
				else
					_lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;



				IsDashing = true;
				IsJumping = false;
				IsWallJumping = false;
				_isJumpCut = false;

				StartCoroutine(nameof(StartDash), _lastDashDir);
			}
			#endregion

			#region SLIDE CHECKS
			if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
				IsSliding = true;
			else
				IsSliding = false;
			#endregion

			#region GRAVITY
			if (!_isDashAttacking)
			{
				//Higher gravity if we've released the jump input or are falling
				if (IsSliding)
				{
					SetGravityScale(0);
				}
				else if (RB.linearVelocity.y < 0 && _moveInput.y < 0)
				{
					//Much higher gravity if holding down
					SetGravityScale(gravityScale * fastFallGravityMult);
					//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
					RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -maxFastFallSpeed));
				}
				else if (_isJumpCut)
				{
					//Higher gravity if jump button released
					SetGravityScale(gravityScale * jumpCutGravityMult);
					RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -maxFallSpeed));
				}
				else if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < jumpHangTimeThreshold)
				{
					SetGravityScale(gravityScale * jumpHangGravityMult);
				}
				else if (RB.linearVelocity.y < 0)
				{
					//Higher gravity if falling
					SetGravityScale(gravityScale * fallGravityMult);
					//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
					RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -maxFallSpeed));
				}
				else
				{
					//Default gravity if standing on a platform or moving upwards
					SetGravityScale(gravityScale);
				}
			}
			else
			{
				//No gravity when dashing (returns to normal once initial dashAttack phase over)
				SetGravityScale(0);
			}
			#endregion
		}

		private void FixedUpdate()
		{
			//Handle Run
			if (!IsDashing)
			{
				if (IsWallJumping)
					Run(wallJumpRunLerp);
				else
					Run(1);
			}
			else if (_isDashAttacking)
			{
				Run(dashEndRunLerp);
			}

			//Handle Slide
			if (IsSliding)
				Slide();
		}

		#region INPUT CALLBACKS
		//Methods which whandle input detected in Update()
		public void OnJumpInput()
		{
			LastPressedJumpTime = jumpInputBufferTime;
		}

		public void OnJumpUpInput()
		{
			if (CanJumpCut() || CanWallJumpCut())
				_isJumpCut = true;
		}

		public void OnDashInput()
		{
			//if actively jumping
			if (IsJumping || _isJumpFalling)
			{
				if (!_isJumpCut) return;
			}

			LastPressedDashTime = dashInputBufferTime;
		}
		#endregion

		#region GENERAL METHODS
		public void SetGravityScale(float scale)
		{
			RB.gravityScale = scale;
		}
		
		
		#endregion

		//MOVEMENT METHODS
		#region RUN METHODS
		private void Run(float lerpAmount)
		{
			//Calculate the direction we want to move in and our desired velocity
			float targetSpeed = _moveInput.x * runMaxSpeed;
			//We can reduce are control using Lerp() this smooths changes to are direction and speed
			targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

			#region Calculate AccelRate
			float accelRate;

			//Gets an acceleration value based on if we are accelerating (includes turning) 
			//or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
			if (LastOnGroundTime > 0)
				accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount : runDeccelAmount;
			else
				accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount * accelInAir : runDeccelAmount * deccelInAir;
			#endregion

			#region Add Bonus Jump Apex Acceleration
			//Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
			if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < jumpHangTimeThreshold)
			{
				accelRate *= jumpHangAccelerationMult;
				targetSpeed *= jumpHangMaxSpeedMult;
			}
			#endregion

			#region Conserve Momentum
			//We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
			if(doConserveMomentum && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
			{
				//Prevent any deceleration from happening, or in other words conserve are current momentum
				//You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
				accelRate = 0; 
			}
			#endregion

			//Calculate difference between current velocity and desired velocity
			float speedDif = targetSpeed - RB.linearVelocity.x;
			//Calculate force along x-axis to apply to thr player

			float movement = speedDif * accelRate;

			//Convert this to a vector and apply to rigidbody
			RB.AddForce(movement * Vector2.right, ForceMode2D.Force);

			/*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
		}

		private void Turn()
		{
			//stores scale and flips the player along the x axis, 
			Vector3 scale = transform.localScale; 
			scale.x *= -1;
			transform.localScale = scale;

			IsFacingRight = !IsFacingRight;
		}
		#endregion

		#region JUMP METHODS
		private void Jump()
		{
			//Ensures we can't call Jump multiple times from one press
			LastPressedJumpTime = 0;
			LastOnGroundTime = 0;

			#region Perform Jump
			//We increase the force applied if we are falling
			//This means we'll always feel like we jump the same amount 
			//(setting the player's Y velocity to 0 beforehand will likely work the same, but I find this more elegant :D)
			float force = jumpForce;
			if (RB.linearVelocity.y < 0)
				force -= RB.linearVelocity.y;

			RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
			#endregion
		}

		private void WallJump(int dir)
		{
			//Ensures we can't call Wall Jump multiple times from one press
			LastPressedJumpTime = 0;
			LastOnGroundTime = 0;
			LastOnWallRightTime = 0;
			LastOnWallLeftTime = 0;

			#region Perform Wall Jump
			Vector2 force = new Vector2(wallJumpHorizontalForce, jumpForce * wallJumpVerticalMult);
			force.x *= dir; //apply force in opposite direction of wall

			if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
				force.x -= RB.linearVelocity.x;

			if (RB.linearVelocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
				force.y -= RB.linearVelocity.y;

			//Unlike in the run we want to use the Impulse mode.
			//The default mode will apply are force instantly ignoring masss
			RB.AddForce(force, ForceMode2D.Impulse);
			#endregion
		}
		#endregion

		#region DASH METHODS
		//Dash Coroutine
		private IEnumerator StartDash(Vector2 dir)
		{
			//Overall this method of dashing aims to mimic Celeste, if you're looking for
			// a more physics-based approach try a method similar to that used in the jump

			// sleeping
			float sleepStartTime = Time.time;
			while (Time.time - sleepStartTime <= dashSleepTime)
			{
				percent = (Time.time - sleepStartTime) / dashSleepTime;
				timeScale = slowTimeCurve.Evaluate(percent); 
				Time.timeScale = timeScale;

				if (UserInput.Instance.JumpButtonReleasedThisFrame)
				{
					break;
				}

				yield return new WaitForSeconds(0);
			}
			Time.timeScale = 1;
			

			LastOnGroundTime = 0;
			LastPressedDashTime = 0;

			float startTime = Time.time;

			_dashesLeft--;
			UpdateDashAmountUI();
			
			_isDashAttacking = true;

			playerSprite.enabled = false;
			bubbleSprite.enabled = true;

			SetGravityScale(0);

			//We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
			while (Time.time - startTime <= dashAttackTime)
			{
				RB.linearVelocity = dir.normalized * dashSpeed;
				//Pauses the loop until the next frame, creating something of a Update loop. 
				//This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
				yield return null;
			}

			startTime = Time.time;

			_isDashAttacking = false;

			//Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
			SetGravityScale(gravityScale);
			RB.linearVelocity = dashEndSpeed * dir.normalized;

			while (Time.time - startTime <= dashEndTime)
			{
				yield return null;
			}

			bubbleSprite.enabled = false;
			playerSprite.enabled = true;

			//Dash over
			IsDashing = false;
		}

		public void StartRefillDash(int amount)
		{
			_dashCanRefill = true;
			if(_dashRefilling) return;

			StartCoroutine(RefillDash(amount));
		}

		public void StopRefillDash()
		{
			_dashCanRefill = false;
		}

		//Short period before the player is able to dash again
		private IEnumerator RefillDash(int amount)
		{
			//SHoet cooldown, so we can't constantly dash along the ground, again this is the implementation in Celeste, feel free to change it up
			_dashRefilling = true;

			float _curTime = 0;
			while (_curTime < dashRefillTime)
			{
				_curTime += Time.deltaTime;
				yield return new WaitForSeconds(0);
				if (!_dashCanRefill)
				{
					_dashRefilling = false;
					yield break;
				}
			}
			
			_dashRefilling = false;
			_dashesLeft = Mathf.Max(_dashesLeft, amount);
			UpdateDashAmountUI();
		}

		public void SetDashAmount(int amount)
		{
			_dashCanRefill = false;
			_dashRefilling = false;
			_dashesLeft = amount;
			UpdateDashAmountUI();
		}
		

		public void UpdateDashAmountUI()
		{
			for (int i = 0; i < bubbleUiImages.Count; i++)
			{
				if (i + 1 <= _dashesLeft)
				{
					bubbleUiImages[i].enabled = true;
				}
				else
				{
					bubbleUiImages[i].enabled = false;
				}
			}
		}
		#endregion

		#region OTHER MOVEMENT METHODS
		private void Slide()
		{
			//We remove the remaining upwards Impulse to prevent upwards sliding
			if(RB.linearVelocity.y > 0)
			{
				RB.AddForce(-RB.linearVelocity.y * Vector2.up,ForceMode2D.Impulse);
			}
	
			//Works the same as the Run but only in the y-axis
			//THis seems to work fine, buit maybe you'll find a better way to implement a slide into this system
			float speedDif = slideSpeed - RB.linearVelocity.y;	
			float movement = speedDif * slideAccel;
			//So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
			//The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
			movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

			RB.AddForce(movement * Vector2.up);
		}
		#endregion


		#region CHECK METHODS
		public void CheckDirectionToFace(bool isMovingRight)
		{
			if (isMovingRight != IsFacingRight)
				Turn();
		}

		private bool CanJump()
		{
			return LastOnGroundTime > 0 && !IsJumping;
		}

		private bool CanWallJump()
		{
			return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!IsWallJumping ||
				(LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
		}

		private bool CanJumpCut()
		{
			return IsJumping && RB.linearVelocity.y > 0;
		}

		private bool CanWallJumpCut()
		{
			return IsWallJumping && RB.linearVelocity.y > 0;
		}

		private bool CanDash()
		{
			if (IsDashing) return false;

			if (IsJumping) return false;

			return _dashesLeft > 0;
		}

		public bool CanSlide()
		{
			if (LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0)
				return true;
			else
				return false;
		}
		#endregion

		public void TeleportTo(Vector3 position)
		{
			transform.position = position;
			RB.linearVelocity = Vector2.zero;
		}


		#region EDITOR METHODS
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
			Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
		}
		#endregion
	}
}

// created by Dawnosaur :D