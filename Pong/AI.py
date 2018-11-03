# -*- coding: utf-8 -*-
import sys
import signal
import time
import datetime
import threading
import atexit

#FILE IMPORT
import Pong as Pong
import NEAT as NEAT

#EXIT HANDLER
def exit_handler():
    AI1.Stop()
    AI2.Stop()
    #AI1.Save()
    #AI2.Save()
    print('Stop Pong AI...')

atexit.register(exit_handler)


#STARTING APPLICATION
if __name__ == '__main__':
    
    print('\nStart Pong AI...')
    second = True

    #PONG INITIALIZE
    Pong.Init()

    #AI CREATE
    AI1 = NEAT.AI('L', 50)
    AI2 = NEAT.AI('R', 100)

    #AI INITIALIZE INPUTS
    Offset = Pong.HEIGHT - Pong.PAD_HEIGHT/2
    AI1.Inputs = [0]
    AI2.Inputs = [0]
    AI1.Startup(['Up', 'Down'])
    AI2.Startup(['Up', 'Down'])

    LastScoreL = 0
    LastScoreR = 0
    LastKeyL = 'x'  #LAST KEY LEFT
    LastKeyR = 'x'  #LAST KEY RIGHT

    while True:
        
        #RESTART
        if(Pong.NeedRestart):
            if(LastScoreL != Pong.ScoreL):      #RIGHT LOSE GAME
                AI2.Score = Pong.TimeR          #SET AI SCORE
                if(second): AI2.Evolution()
                #AI2.Alive = False                
                Pong.TimeR = 0               
            elif(LastScoreR != Pong.ScoreR):    #LEFT LOSE GAME
                AI1.Score = Pong.TimeL          #SET AI SCORE 
                AI1.Evolution()
                #AI1.Alive = False                         
                Pong.TimeL = 0              
                
            
            LastScoreL = Pong.ScoreL
            LastScoreR = Pong.ScoreR
            Pong.NeedRestart = False

        #MOVEMENT DETECTION
        if not (Pong.MovedL): Pong.TimeL = -1
        if not (Pong.MovedR): Pong.TimeR = -1

        #PONG RUN
        ball = Pong.Run()
        ballL = ball[0]/Pong.WIDTH
        ballR = (Pong.WIDTH - ball[0]) / Pong.WIDTH

        #CALCULATE INPUTS
        posL = (Pong.paddle1_pos[1] - ball[1]) / Offset
        posR = (Pong.paddle2_pos[1] - ball[1]) / Offset
        
        if(posL < 0): AI1.Inputs = [-1 - posL]   
        else: AI1.Inputs = [1 - posL]
        if(posR < 0): AI2.Inputs = [-1 - posR]
        else: AI2.Inputs = [1 - posR]

        #GET PADDLE POSITION
        padL = Pong.getPaddleLeft()
        padR = Pong.getPaddleRight()

        padViewL = [padL[0] + Offset, padL[1] + Offset]
        padViewR = [padR[0] + Offset, padR[1] + Offset]
        
        #TO DRAW
        top = 10
        left = 0.32
        right = 0.58
        duration = datetime.datetime.now() - AI1.Start
        Pong.ListDraw.append(Pong.toDraw('Gen:     ' + str(AI1.Pool.generation), Pong.WIDTH * left, Pong.HEIGHT - 56 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Gen:     ' + str(AI2.Pool.generation), Pong.WIDTH * right, Pong.HEIGHT - 56 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Spec:   ' + str(AI1.Pool.currentSpecies + 1) + '/' + str(len(AI1.Pool.species)), Pong.WIDTH * left, Pong.HEIGHT - 42 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Spec:   ' + str(AI2.Pool.currentSpecies + 1) + '/' + str(len(AI2.Pool.species)), Pong.WIDTH * right, Pong.HEIGHT - 42 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Genom: ' + str(AI1.Pool.currentGenome + 1) + '/' + str(len(AI1.Pool.species[AI1.Pool.currentSpecies].genomes)), Pong.WIDTH * left, Pong.HEIGHT - 28 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Genom: ' + str(AI2.Pool.currentGenome + 1) + '/' + str(len(AI2.Pool.species[AI2.Pool.currentSpecies].genomes)), Pong.WIDTH * right, Pong.HEIGHT - 28 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw(str(AI1.Pool.measured) + '%', Pong.WIDTH * left, Pong.HEIGHT - 14 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw(str(AI2.Pool.measured) + '%', Pong.WIDTH * right, Pong.HEIGHT - 14 - top, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Max: ' + str(AI1.Pool.maxFitness), Pong.WIDTH * 0.14, 70, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw('Max: ' + str(AI2.Pool.maxFitness), Pong.WIDTH * 0.80, 70, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw(LastKeyL, Pong.WIDTH * 0.1, Pong.HEIGHT * 0.9, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw(LastKeyR, Pong.WIDTH - Pong.WIDTH * 0.15, Pong.HEIGHT * 0.9, Pong.font_small, Pong.WHITE))
        Pong.ListDraw.append(Pong.toDraw(str(duration)[:str(duration).find('.')], Pong.WIDTH/2.2, Pong.HEIGHT * 0.1, Pong.font_norm, (100, 100, 255)))

        #RUN AI
        key1 = AI1.Run()
        if(second): key2 = AI2.Run() 
        else: key2 = []

        #EXECUTE LEFT
        if(len(key1) == 0): continue
        elif(key1['Up']): Pong.paddle1_vel = -8; LastKeyL = 'Up'
        elif(key1['Down']): Pong.paddle1_vel = 8; LastKeyL = 'Down'
        else: Pong.paddle1_vel = 0; LastKeyL= 'None'

        #EXECUTE RIGHT
        if(len(key2) == 0): continue    
        elif(key2['Up']): Pong.paddle2_vel = -8; LastKeyR = 'Up'
        elif(key2['Down']): Pong.paddle2_vel = 8; LastKeyR = 'Down'
        else: Pong.paddle2_vel = 0; LastKeyR = 'None'

