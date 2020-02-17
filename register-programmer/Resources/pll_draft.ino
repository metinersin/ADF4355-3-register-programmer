/*
  Digital Pot Control

  This example controls an Analog Devices AD5206 digital potentiometer.
  The AD5206 has 6 potentiometer channels. Each channel's pins are labeled
  A - connect this to voltage
  W - this is the pot's wiper, which changes when you set it
  B - connect this to ground.

 The AD5206 is SPI-compatible,and to command it, you send two bytes,
 one with the channel number (0 - 5) and one with the resistance value for the
 channel (0 - 255).

 The circuit:
  * All A pins  of AD5206 connected to +5V
  * All B pins of AD5206 connected to ground
  * An LED and a 220-ohm resisor in series connected from each W pin to ground
  * CS - to digital pin 10  (SS pin)
  * SDI - to digital pin 11 (MOSI pin)
  * CLK - to digital pin 13 (SCK pin)

 created 10 Aug 2010
 by Tom Igoe

 Thanks to Heather Dewey-Hagborg for the original tutorial, 2005

*/


// inslude the SPI library:
#include <SPI.h>

// set pin 10 as the slave select for the digital pot:

const int slaveSelectPin = 10;

void setup() {
  // set the slaveSelectPin as an output:
  pinMode(slaveSelectPin, OUTPUT);
  // initialize SPI:
  SPI.begin();
  SPI.setClockDivider(SPI_CLOCK_DIV128);
  delayMicroseconds(100);
  WriteADF(0x00, 0x01, 0x05, 0x0c); //Reg12
  delayMicroseconds(100);
  WriteADF(0x00, 0x81, 0x20, 0x0b); //Reg11
  delayMicroseconds(100);
  WriteADF(0x60, 0x00 ,0x0f, 0xba); //Reg10
  delayMicroseconds(100);
  WriteADF(0x0b, 0x0a, 0xbe, 0xc9); //Reg9
  delayMicroseconds(100);
  WriteADF(0x1a, 0x69, 0xa6, 0xb8); //Reg8
  delayMicroseconds(100);
  WriteADF(0x10, 0x00, 0x02, 0xe7); //Reg7
  delayMicroseconds(100);
  WriteADF(0x15, 0x04, 0x80, 0xd6); //Reg6
  delayMicroseconds(100);
  WriteADF(0x00, 0x80, 0x00, 0x25); //Reg5
  delayMicroseconds(100);
  WriteADF(0x30, 0x00, 0x8b, 0x84); //Reg4
  delayMicroseconds(100);
  WriteADF(0x00, 0x00 ,0x00, 0x03); //Reg3
  delayMicroseconds(100);
  WriteADF(0x00, 0x00, 0x00, 0x12); //Reg2
  delayMicroseconds(100);
  WriteADF(0x00, 0x00, 0x00, 0x01); //Reg1
  delayMicroseconds(100);
  WriteADF(0x00, 0x20, 0x0c, 0x80); //Reg0
}

void loop() {

}

void WriteADF(int one, int two, int three, int four) {
  // take the SS pin low to select the chip:
  digitalWrite(slaveSelectPin, LOW);
  delayMicroseconds(200);
  //  send in the address and value via SPI:
  SPI.transfer(one);
  SPI.transfer(two);
  SPI.transfer(three);
  SPI.transfer(four);
  delayMicroseconds(200);
  // take the SS pin high to de-select the chip:
  digitalWrite(slaveSelectPin, HIGH);
}
