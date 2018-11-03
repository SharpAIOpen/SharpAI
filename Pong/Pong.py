import random
import pygame
import sys
from pygame import *

pygame.init()
fps = pygame.time.Clock()

#COLORS
WHITE = (255,255,255)
ORANGE = (255,140,0)
GREEN = (0, 255, 0)
BLACK = (0, 0, 0)

#GLOBALS
WIDTH = 600
HEIGHT = 400
BALL_RADIUS = 5
PAD_WIDTH = 8
PAD_HEIGHT = 80
HALF_PAD_WIDTH = PAD_WIDTH // 2
HALF_PAD_HEIGHT = PAD_HEIGHT // 2

#GAME SPEED
FPS = 400       #CHANGE TO REGULATE THE GAME SPEED

font_norm = pygame.font.SysFont('Comic Sans MS', 20)
font_small = pygame.font.SysFont('Comic Sans MS', 10)
ball_pos = [0, 0]
ball_vel = [0, 0]
paddle1_vel = 0
paddle2_vel = 0

TimeL = 0
TimeR = 0
MovedL = False      #MOVED LEFT
MovedR = False      #MOVED RIGHT
LastPosL = []       #LAST POSITION LEFT
LastPosR = []       #LAST POSITION RIGHT

NeedRestart = False

#CANVAS DECLARATION
Canvas = pygame.display.set_mode((WIDTH, HEIGHT), 0, 32)
pygame.display.set_caption('Pong')
ListDraw = [] #TO DRAW LIST


def getPaddleLeft():
    #GET PADDLE LEFT
    padHeight = PAD_HEIGHT/2
    return [int(paddle1_pos[1] - padHeight), int(paddle1_pos[1] + padHeight)]


def getPaddleRight():
    #GET PADDLE RIGHT
    padHeight = PAD_HEIGHT/2
    return [int(paddle2_pos[1] - padHeight), int(paddle2_pos[1] + padHeight)]


# HELPER FUNCTION THAT SPAWNS A BALL, RETURNS A POSITION VECTOR AND A VELOCITY VECTOR
# IF RIGHT IS TRUE, SPAWN TO THE RIGHT, ELSE SPAWN TO THE LEFT
def InitBall(xRight):
    global ball_pos, ball_vel, NeedRestart    #GLOBALS
    ball_pos = [WIDTH // 2, HEIGHT // 2]
    hori = random.randrange(2, 4)
    vert = random.randrange(1, 3)

    if xRight == False:
        hori = - hori

    ball_vel = [hori, -vert]


def Init():
    #DEFINE EVENT HANDLERS
    global paddle1_pos, paddle2_pos, paddle1_vel, paddle2_vel, LastPosL, LastPosR, ScoreL, ScoreR     #THESE ARE GLOBALS
    paddle1_pos = [HALF_PAD_WIDTH - 1, HEIGHT // 2]
    paddle2_pos = [WIDTH + 1 - HALF_PAD_WIDTH, HEIGHT //2]
    LastPosL = paddle1_pos
    LastPosR = paddle2_pos
    ScoreL = 0
    ScoreR = 0

    if random.randrange(0, 2) == 0: InitBall(True)
    else: InitBall(False)


def Draw(xCanvas):
    #DRAW FUNCTION OF CANVAS
    global paddle1_pos, paddle2_pos, ball_pos, ball_vel, MovedL, MovedR, LastPosL, LastPosR, ScoreL, ScoreR, TimeL, TimeR, NeedRestart

    xCanvas.fill(BLACK)
    pygame.draw.line(xCanvas, WHITE, [WIDTH // 2, 0], [WIDTH // 2, HEIGHT], 1)
    pygame.draw.line(xCanvas, WHITE, [PAD_WIDTH, 0], [PAD_WIDTH, HEIGHT], 1)
    pygame.draw.line(xCanvas, WHITE, [WIDTH - PAD_WIDTH, 0], [WIDTH - PAD_WIDTH, HEIGHT], 1)
    pygame.draw.circle(xCanvas, WHITE, [WIDTH // 2, HEIGHT // 2], 70, 1)

    #UPDATE PADDLE'S VERTICAL POSITION, KEEP PADDLE ON THE SCREEN
    if paddle1_pos[1] > HALF_PAD_HEIGHT and paddle1_pos[1] < HEIGHT - HALF_PAD_HEIGHT:
        paddle1_pos[1] += paddle1_vel
    elif paddle1_pos[1] == HALF_PAD_HEIGHT and paddle1_vel > 0:
        paddle1_pos[1] += paddle1_vel
    elif paddle1_pos[1] == HEIGHT - HALF_PAD_HEIGHT and paddle1_vel < 0:
        paddle1_pos[1] += paddle1_vel

    if paddle2_pos[1] > HALF_PAD_HEIGHT and paddle2_pos[1] < HEIGHT - HALF_PAD_HEIGHT:
        paddle2_pos[1] += paddle2_vel
    elif paddle2_pos[1] == HALF_PAD_HEIGHT and paddle2_vel > 0:
        paddle2_pos[1] += paddle2_vel
    elif paddle2_pos[1] == HEIGHT - HALF_PAD_HEIGHT and paddle2_vel < 0:
        paddle2_pos[1] += paddle2_vel

    #MOVEMENT DETECTION
    if(LastPosL[1] != paddle1_pos[1]): MovedL = True
    if(LastPosR[1] != paddle2_pos[1]): MovedR = True
    LastPosL = paddle1_pos[:]
    LastPosR = paddle2_pos[:]

    #UPDATE BALL
    ball_pos[0] += int(ball_vel[0])
    ball_pos[1] += int(ball_vel[1])

    #DRAW PADDLES AND BALL
    pygame.draw.circle(xCanvas, ORANGE, ball_pos, BALL_RADIUS, 0)
    pygame.draw.polygon(xCanvas, GREEN, [[paddle1_pos[0] - HALF_PAD_WIDTH, paddle1_pos[1] - HALF_PAD_HEIGHT],
                                        [paddle1_pos[0] - HALF_PAD_WIDTH, paddle1_pos[1] + HALF_PAD_HEIGHT],
                                        [paddle1_pos[0] + HALF_PAD_WIDTH, paddle1_pos[1] + HALF_PAD_HEIGHT],
                                        [paddle1_pos[0] + HALF_PAD_WIDTH, paddle1_pos[1] - HALF_PAD_HEIGHT]], 0)
    pygame.draw.polygon(xCanvas, GREEN, [[paddle2_pos[0] - HALF_PAD_WIDTH, paddle2_pos[1] - HALF_PAD_HEIGHT],
                                        [paddle2_pos[0] - HALF_PAD_WIDTH, paddle2_pos[1] + HALF_PAD_HEIGHT],
                                        [paddle2_pos[0] + HALF_PAD_WIDTH, paddle2_pos[1] + HALF_PAD_HEIGHT],
                                        [paddle2_pos[0] + HALF_PAD_WIDTH, paddle2_pos[1] - HALF_PAD_HEIGHT]], 0)

    #RAISE TIME
    TimeR += 1
    TimeL += 1

    #BALL COLLISION CHECK ON TOP AND BOTTOM WALLS
    if int(ball_pos[1]) <= BALL_RADIUS:
        ball_vel[1] = - ball_vel[1]
    if int(ball_pos[1]) >= HEIGHT + 1 - BALL_RADIUS:
        ball_vel[1] = -ball_vel[1]

    #BALL COLLISON CHECK ON GUTTERS OR PADDLES
    if int(ball_pos[0]) <= BALL_RADIUS + PAD_WIDTH and int(ball_pos[1]) in range(paddle1_pos[1] - HALF_PAD_HEIGHT, paddle1_pos[1] + HALF_PAD_HEIGHT, 1):
        ball_vel[0] = -ball_vel[0]
        ball_vel[0] *= 1.1
        ball_vel[1] *= 1.1
    elif int(ball_pos[0]) <= BALL_RADIUS + PAD_WIDTH:
        ScoreR += 1
        InitBall(True)
        MovedL = False
        NeedRestart = True

    if int(ball_pos[0]) >= WIDTH + 1 - BALL_RADIUS - PAD_WIDTH and int(ball_pos[1]) in range(
            paddle2_pos[1] - HALF_PAD_HEIGHT, paddle2_pos[1] + HALF_PAD_HEIGHT, 1):
        ball_vel[0] = -ball_vel[0]
        ball_vel[0] *= 1.1
        ball_vel[1] *= 1.1
    elif int(ball_pos[0]) >= WIDTH + 1 - BALL_RADIUS - PAD_WIDTH:
        ScoreL += 1        
        InitBall(False)
        MovedR = False
        NeedRestart = True

    #UPDATE SCORES
    DrawText(xCanvas, 'Score: ' + str(ScoreL), WIDTH * 0.10, 20, font_norm, (255, 255, 0))
    DrawText(xCanvas, 'Time: ' + str(TimeL), WIDTH * 0.14, 50, font_small, WHITE)
    DrawText(xCanvas, 'Score: ' + str(ScoreR), WIDTH * 0.76, 20, font_norm, (255, 255, 0))
    DrawText(xCanvas, 'Time: ' + str(TimeR), WIDTH * 0.8, 50, font_small, WHITE)

    #DISPLAY INFORMATIONS
    DrawText(xCanvas, str(ball_pos[0]) + ', ' + str(ball_pos[1]), ball_pos[0] - 20, ball_pos[1] + 10, font_small, WHITE)
    DrawText(xCanvas, str(paddle1_pos[1]), paddle1_pos[0] + 12, paddle1_pos[1] + PAD_HEIGHT/3, font_small, WHITE)
    DrawText(xCanvas, str(paddle2_pos[1]), paddle2_pos[0] - 24, paddle2_pos[1] + PAD_HEIGHT/3, font_small, WHITE)

    #DRAW LIST
    for item in ListDraw:
        DrawText(xCanvas, item.Text, item.Left, item.Top, item.Font, item.Color)
        ListDraw.remove(item)


def DrawText(xCanvas, xText, xLeft, xTop, xFont, xColor):
    #DRAW TEXT
    label = xFont.render(xText, 1, xColor)
    xCanvas.blit(label, (xLeft, xTop))


def Keydown(xEvent):
    #KEYDOWN HANDLER
    global paddle1_vel, paddle2_vel

    if xEvent.key == K_UP:
        paddle2_vel = -8
    elif xEvent.key == K_DOWN:
        paddle2_vel = 8
    elif xEvent.key == K_z:
        paddle1_vel = -8
    elif xEvent.key == K_s:
        paddle1_vel = 8


def Keyup(xEvent):
    #KEYUP HANDLER
    global paddle1_vel, paddle2_vel

    if xEvent.key in (K_z, K_s):
        paddle1_vel = 0
    elif xEvent.key in (K_UP, K_DOWN):
        paddle2_vel = 0


def Run():
    #RUN
    if not (NeedRestart): 
        Draw(Canvas) #DRAW GAME

    for xEvent in pygame.event.get():

        if xEvent.type == KEYDOWN:
            Keydown(xEvent)
        elif xEvent.type == KEYUP:
            Keyup(xEvent)
        elif xEvent.type == QUIT:
            pygame.quit()
            sys.exit()

    pygame.display.update()
    fps.tick(FPS) #FPS

    return ball_pos


class toDraw():
    #TO DRAW OBJECT
    def __init__(self, xText, xLeft, xTop, xFont, xColor):
        self.Text = xText
        self.Left = xLeft
        self.Top = xTop
        self.Font = xFont
        self.Color = xColor