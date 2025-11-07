/*
 * brazildriver.h
 *
 * Public header file for the test driver.
 */

/*
 * Copyright (C) 2015-20XX Wagner Popov
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

#ifndef _BRAZILDRIVER_H
#define _BRAZILDRIVER_H

#include "brazilmodel.h"

class BrazilUpsDriver: public UpsDriver
{
public:
	BrazilUpsDriver(UPSINFO *ups);
	virtual ~BrazilUpsDriver() {}

	static UpsDriver *Factory(UPSINFO *ups)
	{ return new BrazilUpsDriver(ups); }

	virtual bool get_capabilities();
	virtual bool read_volatile_data();
	virtual bool read_static_data();
	virtual bool check_state();
	virtual bool refresh();
	virtual bool Open();
	virtual bool Close();
	virtual bool setup();
	virtual bool shutdown();
	virtual bool kill_power();
	virtual int getEventsStr(char **events);

	virtual bool programmation(bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes);
	virtual bool turnLineOn(bool turnon);
	virtual bool turnOutputOn(bool turnon);
	virtual bool shutdownAuto();
	virtual bool turnContinueMode();

	BrazilModelAbstract *model;

protected:

	void send(unsigned char *cmd, int size);
	char *strBuffer(unsigned char* buffer, int len);
private:

	int ReadData(bool getevents);

	struct termios _oldtio;
	struct termios _newtio;

	time_t _received;
	unsigned char _buffer[BrazilModelAbstract::BUFFERLEN];  // Buffer
	unsigned int _bufnxt;		// position off next char in buffer to be written
	unsigned int _buffst;		// position off first char in buffer

	bool _autosetup;

	void bufferAdd(unsigned char c);
	void bufferDel(unsigned int len);
	unsigned int  bufferLen();
	unsigned char *bufferGet();

	void setIOCtlRecv();
	void setIOCtlSend();

	static const char *printBits(size_t const size, void const * const ptr);
};


#endif   /* _BRAZIL_DRIVER_H */
