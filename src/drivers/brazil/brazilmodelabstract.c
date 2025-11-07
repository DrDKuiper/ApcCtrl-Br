/*
 * brazilmodels.c
 *
 *  Created on: Nov 8, 2015
 *      Author: wagner popov dos santos
 */

/*
 * Copyright (C) 2015-2016 Wagner Popov dos Santos
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of version 2 of the GNU General
 * Public License as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1335, USA.
 */


#include "apc.h"
#include "brazilbattery.h"

#include "brazilmodel.h"

BrazilModelAbstract::BrazilModelAbstract(unsigned char model){
	this->_eventssize = 0;
	this->vmodel = model;
	this->lock = false;
	this->regulating_relay = 0;
	this->bat = new BrazilBattery();

	for(int i=0 ; i<10 ; i++){
		this->_batvolt[i] = 0;
		this->_outcurrent[i] = 0;
		this->_outactpower[i] = 0;
		this->_outvolt[i] = 0;
	}
}

BrazilModelAbstract::~BrazilModelAbstract(){
	delete this->bat;
}
BrazilModelAbstract *BrazilModelAbstract::newInstance(unsigned char model, double current_expander){
	Dmsg(50, "Instancing specific model, number %03u.\n",model);
	BrazilModelAbstract *value = 0;
	switch(model){
	case 169:
		value = new BrazilModelSolis700(model);
		break;
	case 171:
		value = new BrazilModelSolis1000_a(model);
		break;
	case 173:
		value = new BrazilModelSolis2000_a(model);
		break;
	case 174:
		value = new BrazilModelSolis2000_b(model);
		break;
	case 175:
		value = new BrazilModelSolis3000(model);
		break;
	case 185:
		value = new BrazilModelBackUPS800(model);
		break;
	case 186:
		value = new BrazilModelBackUPS1200(model);
		break;
	case 187:
		value = new BrazilModelBackUPS2200(model);
		break;
	case 188:
		value = new BrazilModelBackUPS2200_22(model);
		break;
	case 189:
		value = new BrazilModelBackUPS700(model);
		break;
	case 190:
		value = new BrazilModelBackUPS1500(model);
		break;
	case 201:
	case 206:
	case 207:
		value = new BrazilModelSolis1000_b(model);
		break;
	default:
		value = 0;
		break;
	}
	value->bat->setBattery(value->getBatteryVoltageNom(), value->getBatteryCurrentNom(), current_expander);
	return value;
}
int BrazilModelAbstract::testBuffer(unsigned char *buffer, unsigned int datasize){
	unsigned int checksum = 0;

	switch(buffer[0]){
	case 185:
	case 186:
	case 187:
	case 188:
	case 189:
	case 190:
		if(datasize == BrazilModelBackUPS::MSGLEN){
			for(int i=0 ; i < 23 ; i++){
				checksum += buffer[i];
			}
			if(checksum > 0 && buffer[23] == (checksum % 256) && buffer[24] == 254){
				return 1;
			}else{
				return -1;
			}
		}else{
			return 0;
		}
		break;

	case 169:
	case 171:
	case 175:
	case 174:
	case 173:
	case 201:
	case 206:
	case 207:
		if(datasize == BrazilModelSolis::MSGLEN){
			for(int i=0 ; i < 23 ; i++){
				checksum += buffer[i];
			}
			if(checksum > 0 && buffer[23] == (checksum % 256) && buffer[24] == 254){
				return 1;
			}else{
				return -1;
			}
		}else{
			return 0;
		}
		break;

	default:
		return -1;
	}
	return 0;
}

double BrazilModelAbstract::getBatteryVoltageNom(){
	return this->bat->getBatteryVoltageNom();
}

double BrazilModelAbstract::getBatteryLevel(){
	if(this->isLineMode()){
		if(this->isCharging()){
			return 50;
		}else{
			return 100;
		}
	}else{
		return this->bat->calcLevel(this->getBatteryLoad(), this->getBatteryVoltage());
	}
}
void BrazilModelAbstract::refreshVariables(){
	for(int i=9 ; i>0 ; i--){
		this->_batvolt[i] = this->_batvolt[i-1];
		this->_outcurrent[i] = this->_outcurrent[i-1];
		this->_outactpower[i] = this->_outactpower[i-1];
		this->_outvolt[i] = this->_outvolt[i-1];
	}
	this->_batvolt[0] = this->getBatteryVoltageNow();
	this->_outcurrent[0] = this->getOutputCurrentNow();
	this->_outactpower[0] = this->getOutputActivePowerNow();
	this->_outvolt[0] = this->getOutputVoltageNow();
}
double BrazilModelAbstract::getBatteryVoltage(){
	double res = 0;
	int i = 0;
	while((i<10) && this->_batvolt[i] > 0){
		res += this->_batvolt[i];
		i++;
	}
	if(i>0){
		return res / i;
	}else{
		return 0;
	}
}
double BrazilModelAbstract::getOutputVoltage(){
	double res = 0;
	int i = 0;
	while((i<10) && this->_outvolt[i] > 0){
		res += this->_outvolt[i];
		i++;
	}
	if(i>0){
		return res / i;
	}else{
		return 0;
	}
}
double BrazilModelAbstract::getOutputCurrent(){
	double res = 0;
	int i = 0;
	while((i<10) && this->_outcurrent[i] > 0){
		res += this->_outcurrent[i];
		i++;
	}
	if(i>0){
		return res / i;
	}else{
		return 0;
	}
}
double BrazilModelAbstract::getOutputActivePower(){
	double res = 0;
	int i = 0;
	while((i<10) && this->_outactpower[i] > 0){
		res += this->_outactpower[i];
		i++;
	}
	if(i>0){
		return res / i;
	}else{
		return 0;
	}
}
double BrazilModelAbstract::getBatteryTimeLeft(){
	if(this->isLineMode()){
		/*
		 * Caso o nobreak esteja alimentado, o único parâmetro que podemos utilizar é a corrente. Já que a tensão na bateria
		 * é a tensão de flutuação (carregamento constante. Nessa condição vamos utilizar a "Peukert's law". Cabe uma observação
		 * de que a Peukert's law só é uma boa aproximação com descarregamento constante.
		 *
		 */
		return bat->calcTimeLeftPeukert(this->getBatteryLoad())*this->getInverterEfficiency()*(this->isCharging() ? 0.5 : 1);
	}else{
		return bat->calcTimeLeft(this->getBatteryLoad(),this->getBatteryVoltage())*this->getInverterEfficiency();
	}
}
double BrazilModelAbstract::getBatteryVoltageExpectedInitial(){
	return this->bat->calcVoltageMax(this->getBatteryLoad());
}
double BrazilModelAbstract::getBatteryVoltageExpectedFinal(){
	return this->bat->calcVoltageMin(this->getBatteryLoad());
}
double BrazilModelAbstract::getBatteryLoad(){
	return this->bat->calcBatteryLoadC1(this->getOutputActivePower());
}

void BrazilModelAbstract::setBufferLock(){
	this->lock = true;
}

void BrazilModelAbstract::setBufferUnlock(){
	this->lock = false;
}

