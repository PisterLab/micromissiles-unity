import{_ as e,c as i,a2 as a,o as r}from"./chunks/framework.DYF6f1aH.js";const o="/micromissiles-unity/assets/proportional_navigation.g7kij0xV.png",p=JSON.parse('{"title":"Simulator Overview","description":"","frontmatter":{},"headers":[],"relativePath":"Simulator_Overview.md","filePath":"Simulator_Overview.md"}'),n={name:"Simulator_Overview.md"};function s(l,t,c,u,m,d){return r(),i("div",null,t[0]||(t[0]=[a('<h1 id="simulator-overview" tabindex="-1">Simulator Overview <a class="header-anchor" href="#simulator-overview" aria-label="Permalink to &quot;Simulator Overview&quot;">​</a></h1><h2 id="introduction" tabindex="-1">Introduction <a class="header-anchor" href="#introduction" aria-label="Permalink to &quot;Introduction&quot;">​</a></h2><p>Interceptors:</p><ul><li>Carrier interceptors: interceptors that carry and dispense other interceptors (e.g., Hydra-70)</li><li>Missile interceptors: interceptors that pursue threats (e.g., micromissiles)</li></ul><p>Threats:</p><ul><li>Fixed-wing threats: Pursue their targets using proportional navigation (PN)</li><li>Rotary-wing threats: Pursue their targets using direct linear guidance</li></ul><h2 id="simulator-physics" tabindex="-1">Simulator Physics <a class="header-anchor" href="#simulator-physics" aria-label="Permalink to &quot;Simulator Physics&quot;">​</a></h2><p>Agents are modeled as a point mass (3-DOF simulation that ignores rotations) with instantaneous acceleration (no sensing delay, no actuation delay, no airframe delay).</p><ul><li><p>We do not model the aerodynamics of the agents (including the angle of attack)</p></li><li><p>The input to the system is the instantaneous acceleration</p></li><li><p><strong>State vector</strong>: [ \\vec{x}(t) = \\begin{bmatrix} \\vec{p}(t) \\ \\vec{v}(t) \\end{bmatrix} \\in \\mathbb{R}^6 ]</p></li><li><p><strong>State evolution equation</strong>: [ \\frac{d}{dt} \\vec{x}(t) = \\begin{bmatrix} \\vec{a}(t) - \\begin{bmatrix} 0 \\ 0 \\ g \\end{bmatrix} - \\left( \\frac{F_D(\\vec{v}(t))}{m} + \\frac{|\\vec{a}(t) + \\text{proj}_{\\vec{v}(t)}\\begin{bmatrix} 0 \\ 0 \\ g \\end{bmatrix}|}{(L/D)} \\right) \\frac{\\vec{v}(t)}{|\\vec{v}(t)|} \\end{bmatrix} ]</p><ul><li><strong>Acceleration input</strong>: (\\vec{a}(t))</li><li><strong>Gravity</strong>: (\\begin{bmatrix} 0 \\ 0 \\ g \\end{bmatrix})</li><li><strong>Air drag</strong>: (\\frac{F_D(\\vec{v}(t))}{m})</li><li><strong>Lift-induced drag</strong>: (\\frac{|\\vec{a}(t) + \\text{proj}_{\\vec{v}(t)}\\begin{bmatrix} 0 \\ 0 \\ g \\end{bmatrix}|}{(L/D)})</li></ul></li></ul><h2 id="simulator-behaviors" tabindex="-1">Simulator Behaviors <a class="header-anchor" href="#simulator-behaviors" aria-label="Permalink to &quot;Simulator Behaviors&quot;">​</a></h2><p><img src="'+o+'" alt="Proportional Navigation"></p>',11)]))}const h=e(n,[["render",s]]);export{p as __pageData,h as default};