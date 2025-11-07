/*
 * BrazilModelBackUPS700.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelBackUPS700::BrazilModelBackUPS700(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.70;
	this->TENSAO_ENTRADA_F2 = 1.72;
	this->TENSAO_BATERIA_F1 = 0.07;
	this->TENSAO_BATERIA_F2 = 0.60;
	this->CORRENTE_SAIDA_F1_MR = 0.10;
	this->CORRENTE_SAIDA_F2_MR = 0.44;
	this->CORRENTE_SAIDA_F1_MI = 0.10;
	this->CORRENTE_SAIDA_F2_MI = 0.42;

	this->TENSAO_SAIDA_F1_MR[0] = 0.70;
	this->TENSAO_SAIDA_F1_MR[1] = 0.84;
	this->TENSAO_SAIDA_F1_MR[2] = 0.0;
	this->TENSAO_SAIDA_F1_MR[3] = 0.0;
	this->TENSAO_SAIDA_F1_MR[4] = 0.0;
	this->TENSAO_SAIDA_F1_MR[5] = 0.83;

	this->TENSAO_SAIDA_F2_MR[0] = 14.0;
	this->TENSAO_SAIDA_F2_MR[1] = 12.13;
	this->TENSAO_SAIDA_F2_MR[2] = 0.0;
	this->TENSAO_SAIDA_F2_MR[3] = 0.0;
	this->TENSAO_SAIDA_F2_MR[4] = 0.0;
	this->TENSAO_SAIDA_F2_MR[5] = 12.50;
}

const unsigned int BrazilModelBackUPS700::getRegulatingRelay(){
	int r = ((this->_buffer[6] & 0x38) >> 3);
	if(r<0 || r>5){
		Dmsg(1, "BrazilModelBackUPS error! Cannot get Regulatin Relay between 0 and 5.");
		return 3;
	}
	return r;
}
double BrazilModelBackUPS700::getOutputActivePowerNom(){
	return 300;
}
double BrazilModelBackUPS700::getOutputPowerNom(){
	return 700;
}
int BrazilModelBackUPS700::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS700::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS700::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS700::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS700::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS700::getBatteryVoltageNom(){
	return 12;
}
double BrazilModelBackUPS700::getBatteryCurrentNom(){
	return 7;
}
double BrazilModelBackUPS700::getInverterEfficiency(){
	return 0.7;
}

char *BrazilModelBackUPS700::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 700 (code %03d)",this->vmodel);
	return name;
}


