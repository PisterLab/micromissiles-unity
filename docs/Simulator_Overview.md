# Simulator Overview


## Introduction


Interceptors:
- Carrier interceptors: interceptors that carry and dispense other interceptors (e.g., Hydra-70)
- Missile interceptors: interceptors that pursue threats (e.g., micromissiles)

Threats:
- Fixed-wing threats: Pursue their targets using proportional navigation (PN)
- Rotary-wing threats: Pursue their targets using direct linear guidance



## Simulator Physics

Agents are modeled as a point mass (3-DOF simulation that ignores rotations) with instantaneous acceleration (no sensing delay, no actuation delay, no airframe delay).
- We do not model the aerodynamics of the agents (including the angle of attack)
- The input to the system is the instantaneous acceleration

- **State vector**: 
  \[
  \vec{x}(t) = \begin{bmatrix} \vec{p}(t) \\ \vec{v}(t) \end{bmatrix} \in \mathbb{R}^6
  \]

- **State evolution equation**: 
  \[
  \frac{d}{dt} \vec{x}(t) = 
  \begin{bmatrix} 
  \vec{a}(t) - \begin{bmatrix} 0 \\ 0 \\ g \end{bmatrix} - \left( \frac{F_D(\vec{v}(t))}{m} + \frac{\|\vec{a}(t) + \text{proj}_{\vec{v}(t)}\begin{bmatrix} 0 \\ 0 \\ g \end{bmatrix}\|}{(L/D)} \right) \frac{\vec{v}(t)}{\|\vec{v}(t)\|}
  \end{bmatrix}
  \]

  - **Acceleration input**: \(\vec{a}(t)\)
  - **Gravity**: \(\begin{bmatrix} 0 \\ 0 \\ g \end{bmatrix}\)
  - **Air drag**: \(\frac{F_D(\vec{v}(t))}{m}\)
  - **Lift-induced drag**: \(\frac{\|\vec{a}(t) + \text{proj}_{\vec{v}(t)}\begin{bmatrix} 0 \\ 0 \\ g \end{bmatrix}\|}{(L/D)}\)



## Simulator Behaviors


![Proportional Navigation](./images/proportional_navigation.png)